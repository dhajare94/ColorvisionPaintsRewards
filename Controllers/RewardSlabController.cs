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
        public async Task<IActionResult> Create([FromForm] string name, [FromForm] decimal minAmount, [FromForm] decimal maxAmount)
        {
            var slab = new RewardSlab { Name = name, MinAmount = minAmount, MaxAmount = maxAmount };
            await _slabService.CreateAsync(slab);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, [FromForm] string name, [FromForm] decimal minAmount, [FromForm] decimal maxAmount)
        {
            var existing = await _slabService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name = name;
            existing.MinAmount = minAmount;
            existing.MaxAmount = maxAmount;
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
