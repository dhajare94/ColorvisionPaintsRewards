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
        public async Task<IActionResult> Create([FromForm] string name, [FromForm] decimal totalBudget, [FromForm] string validity, [FromForm] string campaignId, 
            [FromForm] string allowedRewardValues, [FromForm] string rewardWeights, [FromForm] int maxHighRewardCount, 
            [FromForm] bool zeroRewardAllowed, [FromForm] bool isReusable, [FromForm] string randomMode)
        {
            var slab = new RewardSlab { 
                Name = name, 
                TotalBudget = totalBudget, 
                Validity = validity ?? "",
                CampaignId = campaignId ?? "",
                AllowedRewardValues = allowedRewardValues ?? "0,2,5,10,20,50",
                RewardWeights = rewardWeights ?? "",
                MaxHighRewardCount = maxHighRewardCount,
                ZeroRewardAllowed = zeroRewardAllowed,
                IsReusable = isReusable,
                RandomMode = randomMode ?? "Curiosity",
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
            await _slabService.CreateAsync(slab);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, [FromForm] string name, [FromForm] decimal totalBudget, [FromForm] string validity, [FromForm] string campaignId,
            [FromForm] string allowedRewardValues, [FromForm] string rewardWeights, [FromForm] int maxHighRewardCount, 
            [FromForm] bool zeroRewardAllowed, [FromForm] bool isReusable, [FromForm] string randomMode)
        {
            var existing = await _slabService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name = name;
            existing.TotalBudget = totalBudget;
            existing.Validity = validity ?? "";
            existing.CampaignId = campaignId ?? "";
            existing.AllowedRewardValues = allowedRewardValues ?? "0,2,5,10,20,50";
            existing.RewardWeights = rewardWeights ?? "";
            existing.MaxHighRewardCount = maxHighRewardCount;
            existing.ZeroRewardAllowed = zeroRewardAllowed;
            existing.IsReusable = isReusable;
            existing.RandomMode = randomMode ?? "Curiosity";

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
