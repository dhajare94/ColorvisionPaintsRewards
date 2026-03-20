using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class CodesController : Controller
    {
        private readonly CodeService _codeService;
        private readonly CampaignService _campaignService;

        public CodesController(CodeService codeService, CampaignService campaignService)
        {
            _codeService = codeService;
            _campaignService = campaignService;
        }

        public async Task<IActionResult> Index(string? campaignId, string? batchName, string? status)
        {
            var codes = await _codeService.GetFilteredAsync(campaignId, null, status);
            var allCodes = await _codeService.GetAllAsync();
            var campaigns = await _campaignService.GetAllAsync();
            
            if (!string.IsNullOrEmpty(batchName))
                codes = codes.Where(c => c.BatchName == batchName || c.BatchId == batchName).ToList();

            ViewBag.Campaigns = campaigns;
            ViewBag.SelectedCampaign = campaignId;
            ViewBag.SelectedBatchName = batchName;
            ViewBag.SelectedStatus = status;
            ViewBag.UniqueBatchNames = allCodes
                .Select(c => !string.IsNullOrEmpty(c.BatchName) ? c.BatchName : c.BatchId)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            return View(codes.OrderByDescending(c => c.CreatedDate).ToList());
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _codeService.DeleteAsync(id);
            TempData["Message"] = "Code deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> BulkDelete(string[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                foreach (var id in selectedIds)
                {
                    await _codeService.DeleteAsync(id);
                }
                TempData["Message"] = $"Successfully deleted {selectedIds.Length} codes.";
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> PrintBatch(string? campaignId, string? batchName, string? status)
        {
            var codes = await _codeService.GetFilteredAsync(campaignId, null, status);
            var campaigns = await _campaignService.GetAllAsync();
            
            if (!string.IsNullOrEmpty(batchName))
                codes = codes.Where(c => c.BatchName == batchName || c.BatchId == batchName).ToList();

            ViewBag.Campaigns = campaigns;
            return View(codes.OrderByDescending(c => c.CreatedDate).ToList());
        }
    }
}
