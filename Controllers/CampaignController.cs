using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class CampaignController : Controller
    {
        private readonly CampaignService _campaignService;
        private readonly RewardSlabService _slabService;

        public CampaignController(CampaignService campaignService, RewardSlabService slabService)
        {
            _campaignService = campaignService;
            _slabService = slabService;
        }

        public async Task<IActionResult> Index()
        {
            var campaigns = await _campaignService.GetAllAsync();
            var slabs = await _slabService.GetAllAsync();
            ViewBag.Slabs = slabs;
            return View(campaigns.OrderByDescending(c => c.CreatedAt).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Campaign campaign)
        {
            await _campaignService.CreateAsync(campaign);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, Campaign campaign)
        {
            campaign.Id = id;
            await _campaignService.UpdateAsync(id, campaign);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SetStatus(string id, string status)
        {
            await _campaignService.SetStatusAsync(id, status);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _campaignService.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCampaign(string id)
        {
            var campaign = await _campaignService.GetByIdAsync(id);
            if (campaign == null) return NotFound();
            return Json(campaign);
        }
    }
}
