using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly RedemptionService _redemptionService;

        public NotificationViewComponent(RedemptionService redemptionService)
        {
            _redemptionService = redemptionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var redemptions = await _redemptionService.GetFilteredAsync(null, "Pending");
            return View(redemptions.Count);
        }
    }
}
