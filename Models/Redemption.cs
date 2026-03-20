namespace QRRewardPlatform.Models
{
    public class Redemption
    {
        public string Id { get; set; } = string.Empty;
        public string CodeId { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string UpiNumber { get; set; } = string.Empty;
        public decimal RewardAmount { get; set; }
        public string RedemptionDate { get; set; } = string.Empty;
        public string PayoutStatus { get; set; } = "Pending"; // Pending / Paid
        public string PayoutBatchId { get; set; } = string.Empty;
        public string RedemptionBatchId { get; set; } = string.Empty; // Group ID
    }
}
