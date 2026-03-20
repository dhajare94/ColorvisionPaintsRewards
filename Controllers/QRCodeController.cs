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
        private readonly SettingsService _settingsService;
        private readonly QRCodeGeneratorService _qrService;
        private readonly ImgBBService _imgbbService;
        public QRCodeController(CodeService codeService, CampaignService campaignService,
            SettingsService settingsService, QRCodeGeneratorService qrService, ImgBBService imgbbService)
        {
            _codeService = codeService;
            _campaignService = campaignService;
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
        public async Task<IActionResult> Generate(string campaignId, int count, string batchName)
        {
            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            var batchId = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var generatedIds = await _codeService.GenerateCodesAsync(campaignId, count, baseUrl, batchId, batchName);

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
