using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace QRRewardPlatform.Controllers
{
    public class CustomerDashboardController : Controller
    {
        private readonly CustomerService _customerService;
        private readonly RedemptionService _redemptionService;
        private readonly IConfiguration _config;

        public CustomerDashboardController(CustomerService customerService, RedemptionService redemptionService, IConfiguration config)
        {
            _customerService = customerService;
            _redemptionService = redemptionService;
            _config = config;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string mobileNumber)
        {
            if (string.IsNullOrEmpty(mobileNumber)) return View();

            var customer = await _customerService.GetByMobileAsync(mobileNumber);
            if (customer != null)
            {
                HttpContext.Session.SetString("CustomerMobile", mobileNumber);
                return RedirectToAction("Index");
            }

            // Redirect to Redeem Page with pre-filled number if not exists
            return RedirectToAction("Index", "Redeem", new { mobileNumber = mobileNumber });
        }

        public async Task<IActionResult> Index()
        {
            var mobile = HttpContext.Session.GetString("CustomerMobile");
            if (string.IsNullOrEmpty(mobile)) return RedirectToAction("Login");

            var customer = await _customerService.GetByMobileAsync(mobile);
            if (customer == null) return RedirectToAction("Login");

            var allRedemptions = await _redemptionService.GetAllAsync();
            var customerRedemptions = allRedemptions
                .Where(r => r.MobileNumber == mobile)
                .OrderByDescending(r => r.RedemptionDate)
                .ToList();

            ViewBag.Customer = customer;
            ViewBag.TotalPending = customerRedemptions.Where(r => r.PayoutStatus == "Pending").Sum(r => r.RewardAmount);
            ViewBag.TotalPaid = customerRedemptions.Where(r => r.PayoutStatus == "Paid").Sum(r => r.RewardAmount);
            ViewBag.TotalCount = customerRedemptions.Count;

            return View(customerRedemptions);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("CustomerMobile");
            return RedirectToAction("Login");
        }

        [HttpPost("/api/auth/verify-msg91-token")]
        public async Task<IActionResult> VerifyMsg91Token([FromBody] TokenRequest request)
        {
            if (string.IsNullOrEmpty(request?.AccessToken))
                return Json(new { success = false, message = "Access token is required." });

            try
            {
                var authKey = _config["Msg91AuthKey"];
                if (string.IsNullOrEmpty(authKey))
                    return Json(new { success = false, message = "Server configuration error: MSG91 Auth Key missing." });

                using var client = new HttpClient();
                var verifyRequest = new Msg91VerifyRequest
                {
                    AuthKey = authKey,
                    AccessToken = request.AccessToken
                };

                var response = await client.PostAsJsonAsync("https://control.msg91.com/api/v5/widget/verifyAccessToken", verifyRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"MSG91 API error ({response.StatusCode}): {errorContent}" });
                }

                var result = await response.Content.ReadFromJsonAsync<Msg91Response>();
                
                if (result == null || (result.Status?.ToLower() != "success" && result.Type?.ToLower() != "success"))
                {
                    return Json(new { 
                        success = false, 
                        message = result?.Message ?? "Invalid or expired token from MSG91." 
                    });
                }

                // MSG91 returns mobile number in different fields depending on config
                var mobile = result.MobileNumber ?? 
                             result.Data?.Mobile ?? 
                             result.Mobile ?? 
                             result.Data?.MobileNumber ??
                             result.Message;

                if (string.IsNullOrEmpty(mobile))
                {
                    return Json(new { success = false, message = "Verification succeeded but mobile number was not found in MSG91 response." });
                }

                // Clean the mobile number
                mobile = NormalizeMobile(mobile);
                if (mobile.Length > 10) mobile = mobile.Substring(mobile.Length - 10);

                var customer = await _customerService.GetByMobileAsync(mobile);
                bool exists = customer != null;

                if (exists)
                {
                    HttpContext.Session.SetString("CustomerMobile", mobile);
                    return Json(new { 
                        success = true, 
                        exists = true, 
                        mobile = mobile, 
                        redirectTo = "/CustomerDashboard" 
                    });
                }
                else
                {
                    HttpContext.Session.SetString("MobileNumber", mobile);
                    HttpContext.Session.SetString("InstagramFollowed", "true");
                    
                    return Json(new { 
                        success = true, 
                        exists = false, 
                        mobile = mobile, 
                        redirectTo = "/Redeem" 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An internal error occurred: " + ex.Message });
            }
        }

        private string NormalizeMobile(string mobile)
        {
            if (string.IsNullOrEmpty(mobile)) return mobile;
            mobile = mobile.Trim().Replace("+", "").Replace(" ", "");
            // If it's a 12-digit number starting with 91, strip the 91
            if (mobile.StartsWith("91") && mobile.Length == 12)
                return mobile.Substring(2);
            return mobile;
        }

        public class Msg91VerifyRequest
        {
            [JsonPropertyName("authkey")]
            public string AuthKey { get; set; } = string.Empty;

            [JsonPropertyName("access-token")]
            public string AccessToken { get; set; } = string.Empty;
        }

        public class TokenRequest
        {
            public string AccessToken { get; set; } = string.Empty;
        }

        public class Msg91Response
        {
            [JsonPropertyName("status")]
            public string? Status { get; set; }
            
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            
            [JsonPropertyName("message")]
            public string? Message { get; set; }
            
            [JsonPropertyName("mobile_number")]
            public string? MobileNumber { get; set; }
            
            [JsonPropertyName("mobile")]
            public string? Mobile { get; set; }
            
            [JsonPropertyName("data")]
            public Msg91Data? Data { get; set; }
        }

        public class Msg91Data
        {
            [JsonPropertyName("mobile")]
            public string? Mobile { get; set; }

            [JsonPropertyName("mobile_number")]
            public string? MobileNumber { get; set; }
        }
    }
}
