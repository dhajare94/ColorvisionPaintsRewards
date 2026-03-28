namespace QRRewardPlatform.Models
{
    public class Campaign
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active / Inactive
        public string RewardSlabId { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
