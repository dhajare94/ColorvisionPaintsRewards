using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;
using System.IO.Compression;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class QRCodeController : Controller
    {
        private readonly CodeService _codeService;
        private readonly CampaignService _campaignService;
        private readonly RewardSlabService _slabService;
        private readonly SettingsService _settingsService;
        private readonly QRCodeGeneratorService _qrService;
        private readonly ImgBBService _imgbbService;

        public QRCodeController(CodeService codeService, CampaignService campaignService,
            RewardSlabService slabService,
            SettingsService settingsService, QRCodeGeneratorService qrService, ImgBBService imgbbService)
        {
            _codeService = codeService;
            _campaignService = campaignService;
            _slabService = slabService;
            _settingsService = settingsService;
            _qrService = qrService;
            _imgbbService = imgbbService;
        }

        public async Task<IActionResult> Index()
        {
            var campaigns = await _campaignService.GetAllAsync();
            ViewBag.Campaigns = campaigns;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(string campaignId, int count, string batchName, string budgetId, string distribution)
        {
            var budget = await _slabService.GetByIdAsync(budgetId);
            if (budget == null) {
                TempData["Error"] = "Budget not found.";
                return RedirectToAction("Index", "Codes");
            }
            decimal availableBudget = budget.TotalBudget - budget.AllocatedAmount;
            if (availableBudget <= 0) {
                TempData["Error"] = "This budget is fully exhausted. No remaining balance to distribute.";
                return RedirectToAction("Index", "Codes");
            }

            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            var batchId = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Distribute budget
            var amounts = new List<decimal>();
            if (distribution == "equal") {
                decimal amountPerCode = Math.Floor((availableBudget / count) * 100) / 100;
                if (amountPerCode <= 0) amountPerCode = 0.01m; // ensure min logic
                
                decimal remainder = availableBudget - (amountPerCode * count);
                for (int i=0; i<count; i++) amounts.Add(amountPerCode);
                
                if (remainder >= 0 && remainder < availableBudget) {
                    amounts[0] += remainder; // dump remainder onto first code to ensure total payout exactly matches exhausted amount
                }
            } else {
                // random distribution, ensuring total exactly matches
                Random rnd = new Random();
                var randoms = new List<double>();
                for (int i=0; i<count; i++) randoms.Add(rnd.NextDouble() + 0.1); // add 0.1 to avoid pure zero
                
                double sum = randoms.Sum();
                decimal runningTotal = 0;
                for (int i=0; i<count-1; i++) {
                    decimal amount = Math.Floor(((decimal)(randoms[i] / sum) * availableBudget) * 100) / 100;
                    amounts.Add(amount);
                    runningTotal += amount;
                }
                amounts.Add(availableBudget - runningTotal); // last code gets exactly the rest
            }

            // Shuffle if random
            if (distribution == "random") {
                Random rng = new Random();
                int n = amounts.Count;  
                while (n > 1) {  
                    n--;  
                    int k = rng.Next(n + 1);  
                    decimal value = amounts[k];  
                    amounts[k] = amounts[n];  
                    amounts[n] = value;  
                } 
            }

            var generatedIds = await _codeService.GenerateCodesAsync(campaignId, count, baseUrl, batchId, batchName, budgetId, amounts);

            // Update budget tracker
            budget.AllocatedAmount += amounts.Sum();
            budget.GeneratedCoupons += count;
            await _slabService.UpdateAsync(budget.Id, budget);

            int imgbbSuccessCount = 0;
            foreach (var id in generatedIds)
            {
                var code = await _codeService.GetByIdAsync(id);
                if (code != null)
                {
                    var url = $"{baseUrl}?code={code.Code}";
                    var qrBytes = _qrService.GenerateQRCode(url, code.Code);
                    var imgbbUrl = await _imgbbService.UploadImageAsync(qrBytes, $"QR_{code.Code}");
                    
                    if (!string.IsNullOrEmpty(imgbbUrl))
                    {
                        await _codeService.UpdateQRUrlAsync(id, imgbbUrl);
                        imgbbSuccessCount++;
                    }
                }
            }

            TempData["Message"] = $"Successfully generated {generatedIds.Count} QR codes. Uploaded {imgbbSuccessCount} to ImgBB.";
            TempData["GeneratedBatch"] = batchId;

            return RedirectToAction("Index", "Codes");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQR(string code)
        {
            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            var url = $"{baseUrl}?code={code}";
            var qrBytes = _qrService.GenerateQRCode(url, code);
            return File(qrBytes, "image/png", $"QR_{code}.png");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBatch(string batchId)
        {
            var codes = await _codeService.GetFilteredAsync(null, batchId, null);
            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var code in codes)
                {
                    var url = $"{baseUrl}?code={code.Code}";
                    var qrBytes = _qrService.GenerateQRCode(url, code.Code);
                    var entry = archive.CreateEntry($"QR_{code.Code}.png");
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(qrBytes);
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", $"QRCodes_{batchId}.zip");
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchCodes(string batchId)
        {
            var codes = await _codeService.GetFilteredAsync(null, batchId, null);
            return Json(codes);
        }
    }
}
