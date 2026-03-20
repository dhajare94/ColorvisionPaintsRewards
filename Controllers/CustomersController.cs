using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly CustomerService _customerService;
        private readonly RedemptionService _redemptionService;
        private readonly CampaignService _campaignService;

        public CustomersController(CustomerService customerService, RedemptionService redemptionService, CampaignService campaignService)
        {
            _customerService = customerService;
            _redemptionService = redemptionService;
            _campaignService = campaignService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllAsync();
            return View(customers.OrderByDescending(c => c.CreatedDate).ToList());
        }

        public IActionResult Create()
        {
            return View(new Customer());
        }

        public async Task<IActionResult> Profile(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();

            var allRedemptions = await _redemptionService.GetAllAsync();
            var customerRedemptions = allRedemptions.Where(r => r.MobileNumber == customer.MobileNumber).OrderByDescending(r => r.RedemptionDate).ToList();

            ViewBag.Redemptions = customerRedemptions;
            ViewBag.TotalBags = customer.TotalBagsRedeemed;
            ViewBag.TotalAmount = customer.TotalRewards;

            var campaigns = await _campaignService.GetAllAsync();
            ViewBag.Campaigns = campaigns;

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.CreateAsync(customer);
                TempData["SuccessMessage"] = "Customer created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Customer customer)
        {
            if (id != customer.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                await _customerService.UpdateAsync(id, customer);
                TempData["SuccessMessage"] = "Customer updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _customerService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Customer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
