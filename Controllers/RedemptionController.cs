using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class RedemptionController : Controller
    {
        private readonly RedemptionService _redemptionService;
        private readonly CampaignService _campaignService;

        public RedemptionController(RedemptionService redemptionService, CampaignService campaignService)
        {
            _redemptionService = redemptionService;
            _campaignService = campaignService;
        }

        public async Task<IActionResult> Index(string? campaignId, string? status)
        {
            var redemptions = await _redemptionService.GetFilteredAsync(campaignId, status);
            var campaigns = await _campaignService.GetAllAsync();
            ViewBag.Campaigns = campaigns;
            ViewBag.SelectedCampaign = campaignId;
            ViewBag.SelectedStatus = status;
            return View(redemptions.OrderByDescending(r => r.RedemptionDate).ToList());
        }
    }
}
