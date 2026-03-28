using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    public class RedeemController : Controller
    {
        private readonly RedemptionService _redemptionService;
        private readonly CodeService _codeService;
        private readonly SettingsService _settingsService;
        private readonly CustomerService _customerService;

        public RedeemController(RedemptionService redemptionService, CodeService codeService, SettingsService settingsService, CustomerService customerService)
        {
            _redemptionService = redemptionService;
            _codeService = codeService;
            _settingsService = settingsService;
            _customerService = customerService;
        }

        public async Task<IActionResult> Index(string? code)
        {
            var settings = await _settingsService.GetSettingsAsync();
            ViewBag.InstagramUrl = settings?.InstagramUrl;

            // Load from session if available
            ViewBag.SessionName = HttpContext.Session.GetString("UserName");
            ViewBag.SessionMobile = HttpContext.Session.GetString("MobileNumber");
            ViewBag.SessionCity = HttpContext.Session.GetString("City");
            ViewBag.SessionDistrict = HttpContext.Session.GetString("District");
            ViewBag.InstagramFollowed = HttpContext.Session.GetString("InstagramFollowed") == "true";

            if (string.IsNullOrEmpty(code))
            {
                ViewBag.Status = "pending";
                return View();
            }

            var codeEntry = await _codeService.GetByCodeAsync(code);
            if (codeEntry == null)
            {
                ViewBag.Status = "invalid";
                ViewBag.Message = "Invalid code. This code does not exist.";
                return View();
            }

            if (codeEntry.Status == "Redeemed")
            {
                ViewBag.Status = "redeemed";
                ViewBag.Message = "This code has already been redeemed.";
                return View();
            }

            ViewBag.Status = "valid";
            ViewBag.Code = code;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomer(string mobileNumber)
        {
            var customer = await _customerService.GetByMobileAsync(mobileNumber);
            if (customer == null) return NotFound();
            return Json(new { 
                name = customer.Name, 
                city = customer.City, 
                district = customer.District, 
                category = customer.Category,
                upiNumber = customer.UpiNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "Code is required." });
            }

            var codeEntry = await _codeService.GetByCodeAsync(code);
            if (codeEntry == null)
            {
                return Json(new { success = false, message = "Invalid code. This code does not exist." });
            }

            if (codeEntry.Status == "Redeemed")
            {
                return Json(new { success = false, message = "This code has already been redeemed." });
            }

            // Get reward amount if possible (dummy value for now if not available in codeEntry)
            // In a real scenario, you'd check the campaign/batch for the reward amount
            return Json(new { success = true, message = "Code is valid!", code = code });
        }

        [HttpPost]
        public async Task<IActionResult> Submit(string? code, string userName, string mobileNumber, string city, string district, string category, string upiNumber)
        {
            var settings = await _settingsService.GetSettingsAsync();
            ViewBag.InstagramUrl = settings?.InstagramUrl;

            // Save to session
            HttpContext.Session.SetString("UserName", userName);
            HttpContext.Session.SetString("MobileNumber", mobileNumber);
            HttpContext.Session.SetString("City", city);
            HttpContext.Session.SetString("District", district);

            bool success;
            string message;
            decimal reward = 0;

            if (string.IsNullOrEmpty(code))
            {
                // Register user only
                await _customerService.GetOrCreateAsync(mobileNumber, userName, city, district, category);
                success = true;
                message = "Customer details registered successfully.";
                ViewBag.Status = "registered";
            }
            else
            {
                var result = await _redemptionService.RedeemCodeAsync(code, userName, mobileNumber, city, district, category, upiNumber);
                success = result.success;
                message = result.message;
                reward = result.reward;
                ViewBag.Code = code;
                ViewBag.Status = success ? "success" : "error";
            }

            ViewBag.Message = message;
            ViewBag.Reward = reward;

            return View("Index");
        }

        [HttpPost]
        public IActionResult SetInstagramFollowed()
        {
            HttpContext.Session.SetString("InstagramFollowed", "true");
            return Ok();
        }
    }
}
