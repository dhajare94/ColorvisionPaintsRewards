namespace QRRewardPlatform.Models
{
    public class RewardSlab
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public bool ZeroRewardAllowed { get; set; } = true;
        public string Date { get; set; } = string.Empty;
        public decimal AllocatedAmount { get; set; }
        public decimal RedeemedAmount { get; set; }
        public int GeneratedCoupons { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
