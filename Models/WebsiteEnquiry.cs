using System;

namespace QRRewardPlatform.Models
{
    public class WebsiteEnquiry
    {
        public string Id { get; set; } = string.Empty; // Firebase Key
        public long EnquiryId { get; set; } // Numeric ID for display
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? RoleType { get; set; }
        public string? ProductInterested { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSlug { get; set; }
        public int Quantity { get; set; } = 1;
        public string? CartJson { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public string? PageUrl { get; set; }
        public string? CreatedFrom { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Status { get; set; } = "New";
        public string? AssignedTo { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
        public string? Remarks { get; set; }
    }
}
