using System.ComponentModel.DataAnnotations;

namespace QRRewardPlatform.Models
{
    public class WebsiteEnquiryDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Mobile { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? City { get; set; }
        public string? State { get; set; }
        public string? RoleType { get; set; }
        public string? ProductInterested { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSlug { get; set; }
        public int? Quantity { get; set; }
        public object? CartItems { get; set; } // Can be array or object
        public string? Message { get; set; }
        public string? Source { get; set; }
        public string? PageUrl { get; set; }
        public string? CreatedFrom { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
