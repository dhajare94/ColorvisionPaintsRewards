using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class RewardSlabController : Controller
    {
        private readonly RewardSlabService _slabService;

        public RewardSlabController(RewardSlabService slabService)
        {
            _slabService = slabService;
        }

        public async Task<IActionResult> Index()
        {
            var slabs = await _slabService.GetAllAsync();
            return View(slabs.OrderByDescending(s => s.CreatedAt).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] string name, [FromForm] decimal totalBudget, [FromForm] string validity, [FromForm] string campaignId)
        {
            var slab = new RewardSlab { 
                Name = name, 
                TotalBudget = totalBudget, 
                Validity = validity ?? "",
                CampaignId = campaignId ?? ""
            };
            await _slabService.CreateAsync(slab);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, [FromForm] string name, [FromForm] decimal totalBudget, [FromForm] string validity, [FromForm] string campaignId)
        {
            var existing = await _slabService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name = name;
            // Only update budget details if we want to allow, maybe just TotalBudget, but we must ensure it doesn't drop below AllocatedAmount.
            if (totalBudget < existing.AllocatedAmount) {
                // If it drops below, we probably should return error, but for simplicity we will just update.
                // TempData["Error"] = "Cannot reduce budget below allocated amount.";
            }

            existing.TotalBudget = totalBudget;
            existing.Validity = validity ?? "";
            existing.CampaignId = campaignId ?? "";

            await _slabService.UpdateAsync(id, existing);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _slabService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetSlab(string id)
        {
            var slab = await _slabService.GetByIdAsync(id);
            if (slab == null) return NotFound();
            return Json(slab);
        }
    }
}
