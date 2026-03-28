using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class PayoutController : Controller
    {
        private readonly PayoutService _payoutService;
        private readonly RedemptionService _redemptionService;

        public PayoutController(PayoutService payoutService, RedemptionService redemptionService)
        {
            _payoutService = payoutService;
            _redemptionService = redemptionService;
        }

        public async Task<IActionResult> Index()
        {
            var batches = await _payoutService.GetAllAsync();
            var pending = await _redemptionService.GetPendingAsync();
            ViewBag.PendingCount = pending.Count;
            ViewBag.PendingAmount = pending.Sum(p => p.RewardAmount);
            return View(batches.OrderByDescending(b => b.CreatedDate).ToList());
        }


        [HttpPost]
        public async Task<IActionResult> CreateBatch()
        {
            try
            {
                var batchId = await _payoutService.CreateBatchAsync();
                if (string.IsNullOrEmpty(batchId))
                    TempData["Error"] = "No pending redemptions to process.";
                else
                    TempData["Message"] = "Payout batch created successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create payout batch: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(string id)
        {
            var bytes = await _payoutService.ExportCsvAsync(id);
            return File(bytes, "text/csv", $"Payout_{id}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string id)
        {
            var bytes = await _payoutService.ExportExcelAsync(id);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Payout_{id}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> MarkComplete(string id)
        {
            await _payoutService.MarkCompletedAsync(id);
            TempData["Message"] = "Payout batch marked as completed.";
            return RedirectToAction("Index");
        }
    }
}
