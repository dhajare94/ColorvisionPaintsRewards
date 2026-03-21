namespace QRRewardPlatform.Models
{
    public class RedemptionCode
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string BudgetId { get; set; } = string.Empty; // Added BudgetId for traceability
        public decimal RewardAmount { get; set; } // Added RewardAmount 
        public string Status { get; set; } = "Unused"; // Unused / Redeemed
        public string CreatedDate { get; set; } = string.Empty;
        public string QRUrl { get; set; } = string.Empty;
        public string RedeemedAt { get; set; } = string.Empty;
        public string RedeemedBy { get; set; } = string.Empty; // redemptionId
    }
}
