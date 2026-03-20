using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;
using System.Security.Claims;

namespace QRRewardPlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Please enter username and password.";
                return View(model);
            }

            var admin = await _authService.ValidateLoginAsync(model.Username, model.Password);
            if (admin == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, admin.Username),
                new(ClaimTypes.Role, admin.Role),
                new("DisplayName", admin.DisplayName)
            };

            if (!string.IsNullOrEmpty(admin.ProfileImage))
            {
                claims.Add(new Claim("ProfileImage", admin.ProfileImage));
            }
            if (admin.Permissions != null && admin.Permissions.Any())
            {
                claims.Add(new Claim("Permissions", string.Join(",", admin.Permissions)));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
