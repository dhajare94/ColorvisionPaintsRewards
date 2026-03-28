namespace QRRewardPlatform.Models
{
    public class AdminUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
        public string CreatedAt { get; set; } = string.Empty;
        public string LastLogin { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class LoginViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
