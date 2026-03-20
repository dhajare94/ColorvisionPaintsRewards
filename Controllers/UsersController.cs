using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly AuthService _authService;
        private readonly ImgBBService _imgBBService;

        public UsersController(AuthService authService, ImgBBService imgBBService)
        {
            _authService = authService;
            _imgBBService = imgBBService;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("Admin") && !User.Claims.Any(c => c.Type == "Permissions" && c.Value.Contains("Users")))
            {
                return Forbid();
            }
            var users = await _authService.GetAllAdminsAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!User.IsInRole("Admin") && !User.Claims.Any(c => c.Type == "Permissions" && c.Value.Contains("Users")))
            {
                return Forbid();
            }
            ViewBag.NextUsername = await _authService.GenerateNextUsernameAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string displayName, string password, string phoneNumber, string email, string role, IFormFile? profileImage, string[]? permissions)
        {
            if (!User.IsInRole("Admin") && !User.Claims.Any(c => c.Type == "Permissions" && c.Value.Contains("Users")))
            {
                return Forbid();
            }
            try
            {
                if (!User.IsInRole("Admin"))
                {
                    role = "Staff"; // Force role to Staff if a non-Admin creates it
                }
                
                var username = await _authService.GenerateNextUsernameAsync();
                string? imageUrl = null;
                
                if (profileImage != null && profileImage.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await profileImage.CopyToAsync(ms);
                    imageUrl = await _imgBBService.UploadImageAsync(ms.ToArray(), profileImage.FileName);
                }

                await _authService.CreateAdminAsync(username, password, displayName, role, imageUrl, phoneNumber, email, permissions?.ToList());
                
                TempData["Message"] = "User created successfully with username: " + username;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error creating user: " + ex.Message;
                ViewBag.NextUsername = await _authService.GenerateNextUsernameAsync();
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (!User.IsInRole("Admin") && !User.Claims.Any(c => c.Type == "Permissions" && c.Value.Contains("Users")))
            {
                return Forbid();
            }
            var users = await _authService.GetAllAdminsAsync();
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            
            if (user.Role == "Admin" && !User.IsInRole("Admin"))
            {
                return Forbid(); // Non-admins cannot edit Admins
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, string displayName, string phoneNumber, string email, string role, IFormFile? profileImage, string[]? permissions)
        {
            if (!User.IsInRole("Admin") && !User.Claims.Any(c => c.Type == "Permissions" && c.Value.Contains("Users")))
            {
                return Forbid();
            }
            try
            {
                var users = await _authService.GetAllAdminsAsync();
                var existingUser = users.FirstOrDefault(u => u.Id == id);
                if (existingUser == null) return NotFound();

                if (existingUser.Role == "Admin" && !User.IsInRole("Admin"))
                {
                    return Forbid(); // Non-admins cannot maliciously post to edit an Admin
                }

                if (!User.IsInRole("Admin"))
                {
                    role = "Staff"; // Force role to Staff if a non-Admin edits it
                }

                string? imageUrl = existingUser.ProfileImage;
                
                if (profileImage != null && profileImage.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await profileImage.CopyToAsync(ms);
                    imageUrl = await _imgBBService.UploadImageAsync(ms.ToArray(), profileImage.FileName);
                }

                existingUser.DisplayName = displayName;
                existingUser.PhoneNumber = phoneNumber;
                existingUser.Email = email;
                existingUser.Role = role;
                existingUser.ProfileImage = imageUrl;
                existingUser.Permissions = permissions?.ToList() ?? new List<string>();

                await _authService.UpdateAdminAsync(id, existingUser);
                
                TempData["Message"] = "User updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating user: " + ex.Message;
                return RedirectToAction("Edit", new { id });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _authService.DeleteAdminAsync(id);
                TempData["Message"] = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting user: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Unauthorized();

            var user = await _authService.ValidateLoginAsync(username, currentPassword);
            if (user == null)
            {
                TempData["Error"] = "Invalid current password.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            // Since user.Id is set during ValidateLoginAsync, we can use it
            var allUsers = await _authService.GetAllAdminsAsync();
            var currentUser = allUsers.FirstOrDefault(u => u.Username == username);
            if (currentUser != null && currentUser.Id != null)
            {
                await _authService.ChangePasswordAsync(currentUser.Id, newPassword);
                TempData["Message"] = "Password changed successfully.";
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}
