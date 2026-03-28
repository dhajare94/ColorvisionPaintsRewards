using QRRewardPlatform.Models;

namespace QRRewardPlatform.Services
{
    public class AuthService
    {
        private readonly FirebaseService _firebase;

        public AuthService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<AdminUser?> ValidateLoginAsync(string username, string password)
        {
            var admins = await _firebase.GetAllAsync<AdminUser>("admins");
            var admin = admins.Values.FirstOrDefault(a => 
                a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (admin == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
                return null;

            // Update last login
            admin.LastLogin = DateTime.UtcNow.ToString("o");
            var entry = admins.First(a => a.Value.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            admin.Id = entry.Key;
            await _firebase.SetAsync("admins", entry.Key, admin);

            return admin;
        }

        public async Task CreateAdminAsync(string username, string password, string displayName, string role = "Admin", string? profileImage = null, string? phoneNumber = null, string? email = null, List<string>? permissions = null)
        {
            var admin = new AdminUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                DisplayName = displayName,
                Role = role,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastLogin = "",
                ProfileImage = profileImage,
                PhoneNumber = phoneNumber,
                Email = email,
                Permissions = permissions ?? new List<string>()
            };

            await _firebase.PushAsync("admins", admin);
        }

        public async Task UpdateAdminAsync(string id, AdminUser updatedAdmin)
        {
            var admins = await _firebase.GetAllAsync<AdminUser>("admins");
            if (admins.ContainsKey(id))
            {
                var existingAdmin = admins[id];
                existingAdmin.DisplayName = updatedAdmin.DisplayName;
                existingAdmin.Role = updatedAdmin.Role;
                if (!string.IsNullOrEmpty(updatedAdmin.PasswordHash))
                {
                    existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedAdmin.PasswordHash);
                }
                existingAdmin.ProfileImage = updatedAdmin.ProfileImage;
                existingAdmin.PhoneNumber = updatedAdmin.PhoneNumber;
                existingAdmin.Email = updatedAdmin.Email;
                existingAdmin.Permissions = updatedAdmin.Permissions ?? new List<string>();
                
                await _firebase.SetAsync("admins", id, existingAdmin);
            }
        }

        public async Task ChangePasswordAsync(string id, string newPassword)
        {
            var admins = await _firebase.GetAllAsync<AdminUser>("admins");
            if (admins.ContainsKey(id))
            {
                var admin = admins[id];
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _firebase.SetAsync("admins", id, admin);
            }
        }

        public async Task<string> GenerateNextUsernameAsync()
        {
            var admins = await _firebase.GetAllAsync<AdminUser>("admins");
            int maxId = 0;
            
            foreach (var admin in admins.Values)
            {
                if (admin.Username.StartsWith("cvsp", StringComparison.OrdinalIgnoreCase))
                {
                    var numPart = admin.Username.Substring(4);
                    if (int.TryParse(numPart, out int num) && num > maxId)
                    {
                        maxId = num;
                    }
                }
            }
            
            return $"cvsp{maxId + 1}";
        }

        public async Task SeedDefaultAdminAsync()
        {
            var admins = await _firebase.GetAllAsync<AdminUser>("admins");
            
            // Remove old default admin if it exists to strictly follow the new default prompt
            var oldAdmin = admins.FirstOrDefault(a => a.Value.Username == "admin");
            if (oldAdmin.Key != null)
            {
                await _firebase.DeleteNodeAsync($"admins/{oldAdmin.Key}");
            }

            var dhajare94Entry = admins.FirstOrDefault(a => a.Value.Username == "dhajare94");
            if (dhajare94Entry.Key == null)
            {
                await CreateAdminAsync("dhajare94", "Sbr00216@", "Administrator", "Admin");
            }
            else if (!BCrypt.Net.BCrypt.Verify("Sbr00216@", dhajare94Entry.Value.PasswordHash))
            {
                var admin = dhajare94Entry.Value;
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sbr00216@");
                await _firebase.SetAsync("admins", dhajare94Entry.Key, admin);
            }
        }

        public async Task<List<AdminUser>> GetAllAdminsAsync()
        {
            var admins = await _firebase.GetAllAsync<AdminUser>("admins");
            return admins.Select(a => { a.Value.Id = a.Key; return a.Value; }).ToList();
        }

        public async Task DeleteAdminAsync(string id)
        {
            await _firebase.DeleteAsync("admins", id);
        }
    }
}
