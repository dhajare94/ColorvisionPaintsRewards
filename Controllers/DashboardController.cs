using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly CampaignService _campaignService;
        private readonly CodeService _codeService;
        private readonly RedemptionService _redemptionService;
        private readonly PayoutService _payoutService;
        private readonly CustomerService _customerService;
        private readonly FirebaseService _firebaseService;
        
        public DashboardController(CampaignService campaignService, CodeService codeService,
            RedemptionService redemptionService, PayoutService payoutService, CustomerService customerService, FirebaseService firebaseService)
        {
            _campaignService = campaignService;
            _codeService = codeService;
            _redemptionService = redemptionService;
            _payoutService = payoutService;
            _customerService = customerService;
            _firebaseService = firebaseService;
        }

        public async Task<IActionResult> Index()
        {
            var campaigns = await _campaignService.GetAllAsync();
            var codes = await _codeService.GetAllAsync();
            var redemptions = await _redemptionService.GetAllAsync();
            var payouts = await _payoutService.GetAllAsync();
            var customers = await _customerService.GetAllAsync();

            var model = new DashboardViewModel
            {
                TotalCampaigns = campaigns.Count,
                TotalCodes = codes.Count,
                TotalRedemptions = redemptions.Count,
                PendingPayouts = redemptions.Where(r => r.PayoutStatus == "Pending").Sum(r => r.RewardAmount) + payouts.Where(b => b.Status == "Pending").Sum(b => b.TotalAmount),
                CompletedPayouts = payouts.Where(b => b.Status == "Completed").Sum(b => b.TotalAmount),
                RecentRedemptions = redemptions.OrderByDescending(r => r.RedemptionDate).ToList(),
                RecentCampaigns = campaigns.OrderByDescending(c => c.CreatedAt).Take(10).ToList(),
                
                TotalCustomers = customers.Count,
                TopCustomers = customers.OrderByDescending(c => c.TotalBagsRedeemed).Take(5).ToList(),
                CategoryStats = customers.GroupBy(c => string.IsNullOrEmpty(c.Category) ? "Unknown" : c.Category)
                                         .ToDictionary(g => g.Key, g => g.Count()),
                TopCities = customers.GroupBy(c => string.IsNullOrEmpty(c.City) ? "Unknown" : c.City)
                                     .OrderByDescending(g => g.Count())
                                     .Take(5)
                                     .ToDictionary(g => g.Key, g => g.Count())
            };

            // Daily redemptions for last 30 days
            var last30Days = Enumerable.Range(0, 30)
                .Select(i => DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd"))
                .Reverse().ToList();

            foreach (var day in last30Days)
            {
                model.DailyRedemptions[day] = redemptions
                    .Count(r => r.RedemptionDate.StartsWith(day));
            }

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ResetData()
        {
            var codes = await _codeService.GetAllAsync();
            var customers = await _customerService.GetAllAsync();
            
            // Delete all redemptions
            await _firebaseService.DeleteNodeAsync("redemptions");
            await _firebaseService.DeleteNodeAsync("redemptionBatches");
            await _firebaseService.DeleteNodeAsync("payoutBatches");

            foreach(var cust in customers) {
                // Keep the customer but zero out their stats
                cust.TotalBagsRedeemed = 0;
                cust.TotalRewards = 0;
                await _customerService.UpdateAsync(cust.Id, cust);
            }

            int count = 0;
            foreach(var code in codes) {
                if(count >= 100) {
                    await _codeService.DeleteAsync(code.Id);
                } else {
                    if (code.Status != "Unused") {
                        code.Status = "Unused";
                        await _firebaseService.UpdateAsync("codes", code.Id, code);
                    }
                    count++;
                }
            }

            return Content("Data wiped. Kept exactly 100 QR codes, wiped redemptions, and zeroed customer balances.");
        }

        [AllowAnonymous]
        public async Task<IActionResult> ClearPayouts()
        {
            await _firebaseService.DeleteNodeAsync("payoutBatches");
            return Content("Legacy payout batches cleared.");
        }
    }
}
