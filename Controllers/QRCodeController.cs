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
        public async Task<IActionResult> Generate(string campaignId, int count, string batchName, string budgetId)
        {
            var budget = await _slabService.GetByIdAsync(budgetId);
            if (budget == null) {
                TempData["Error"] = "Budget not found.";
                return RedirectToAction("Index", "Codes");
            }

            decimal targetBudget = budget.TotalBudget;

            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            var batchId = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";

            if (count <= 0) {
                TempData["Error"] = "Coupon count must be at least 1.";
                return RedirectToAction("Index", "Codes");
            }

            // ===== SIMPLE RANDOM DISTRIBUTION =====
            // No fixed reward values, no weights, no complex criteria.
            // Just pure randomness scaled to match the budget.

            var rnd = new Random();
            var amounts = new List<decimal>();

            // Step 1: Generate raw random numbers using exponential distribution.
            // This naturally creates curiosity: most values are small, a few are big.
            var rawValues = new double[count];
            for (int i = 0; i < count; i++)
            {
                double u = rnd.NextDouble();
                rawValues[i] = -Math.Log(1.0 - u * 0.999); // exponential distribution
            }

            // Step 2: If zero reward is allowed, randomly assign ~10-25% of coupons as ₹0
            if (budget.ZeroRewardAllowed)
            {
                int zeroCount = rnd.Next((int)(count * 0.10), (int)(count * 0.25) + 1);
                var zeroIndices = Enumerable.Range(0, count).OrderBy(_ => rnd.Next()).Take(zeroCount).ToList();
                foreach (var idx in zeroIndices)
                {
                    rawValues[idx] = 0;
                }
            }

            // Step 3: Scale raw values so they sum to exactly targetBudget
            double rawSum = rawValues.Sum();

            if (rawSum == 0)
            {
                // Edge case: all ended up zero — put entire budget on one random coupon
                for (int i = 0; i < count; i++) amounts.Add(0m);
                amounts[rnd.Next(count)] = targetBudget;
            }
            else
            {
                // Scale and round to whole rupees
                decimal allocated = 0m;
                for (int i = 0; i < count; i++)
                {
                    decimal scaled = Math.Round((decimal)(rawValues[i] / rawSum) * targetBudget, 0);
                    amounts.Add(scaled);
                    allocated += scaled;
                }

                // Step 4: Fix rounding difference by adjusting random coupons ₹1 at a time
                decimal diff = targetBudget - allocated;
                int direction = diff > 0 ? 1 : -1;
                int adjustments = (int)Math.Abs(diff);
                for (int a = 0; a < adjustments; a++)
                {
                    int tries = 0;
                    while (tries < count * 2)
                    {
                        int idx = rnd.Next(count);
                        if (amounts[idx] + direction >= 0)
                        {
                            amounts[idx] += direction;
                            break;
                        }
                        tries++;
                    }
                }
            }

            // Step 5: Final shuffle so there's absolutely no pattern
            int n = amounts.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                (amounts[k], amounts[n]) = (amounts[n], amounts[k]);
            }

            // ===== END DISTRIBUTION =====

            var generatedIds = await _codeService.GenerateCodesAsync(campaignId, count, baseUrl, batchId, batchName, budgetId, amounts);

            // Update lifetime tracking
            budget.AllocatedAmount += targetBudget;
            budget.GeneratedCoupons += count;
            await _slabService.UpdateAsync(budget.Id, budget);

            // Background: generate QR images and upload to ImgBB
            _ = Task.Run(async () => {
                foreach (var id in generatedIds)
                {
                    try {
                        var code = await _codeService.GetByIdAsync(id);
                        if (code != null)
                        {
                            var url = $"{baseUrl}?code={code.Code}";
                            var qrBytes = _qrService.GenerateQRCode(url, code.Code);
                            var imgbbUrl = await _imgbbService.UploadImageAsync(qrBytes, $"QR_{code.Code}");
                            if (!string.IsNullOrEmpty(imgbbUrl))
                            {
                                await _codeService.UpdateQRUrlAsync(id, imgbbUrl);
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Background QR upload failed for {id}: {ex.Message}");
                    }
                    await Task.Delay(100);
                }
            });

            TempData["Message"] = $"Successfully generated {generatedIds.Count} QR codes. QR images will upload in the background.";
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
