using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly SettingsService _settingsService;
        private readonly AuthService _authService;
        private readonly FirebaseService _firebase;

        public SettingsController(SettingsService settingsService, AuthService authService, FirebaseService firebase)
        {
            _settingsService = settingsService;
            _authService = authService;
            _firebase = firebase;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();
            var admins = await _authService.GetAllAdminsAsync();
            ViewBag.Admins = admins;
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Save(AppSettings settings, string currentPassword)
        {
            if (string.IsNullOrEmpty(currentPassword))
            {
                TempData["Error"] = "Password is required to save settings.";
                return RedirectToAction("Index");
            }

            var username = User.Identity?.Name ?? "";
            var valid = await _authService.ValidateLoginAsync(username, currentPassword);
            if (valid == null)
            {
                TempData["Error"] = "Invalid password. Settings were not saved.";
                return RedirectToAction("Index");
            }

            await _settingsService.SaveSettingsAsync(settings);
            TempData["Message"] = "Settings saved successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> FactoryReset(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Password is required for Factory Reset.";
                return RedirectToAction("Index");
            }

            var username = User.Identity?.Name ?? "";
            var valid = await _authService.ValidateLoginAsync(username, password);
            if (valid == null)
            {
                TempData["Error"] = "Invalid password. System reset aborted.";
                return RedirectToAction("Index");
            }

            await _firebase.DeleteNodeAsync("campaigns");
            await _firebase.DeleteNodeAsync("rewardSlabs");
            await _firebase.DeleteNodeAsync("codes");
            await _firebase.DeleteNodeAsync("redemptions");
            await _firebase.DeleteNodeAsync("payoutBatches");
            await _firebase.DeleteNodeAsync("settings");

            TempData["Message"] = "System reset completely.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdmin(string username, string password, string displayName, string role)
        {
            await _authService.CreateAdminAsync(username, password, displayName, role);
            TempData["Message"] = "User created successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            await _authService.DeleteAdminAsync(id);
            TempData["Message"] = "User deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
