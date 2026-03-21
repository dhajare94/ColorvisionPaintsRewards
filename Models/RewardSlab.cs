using System.Collections.Generic;

namespace QRRewardPlatform.Models
{
    public class RewardSlab // Treated as 'Budget' in UI and Logic now
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty; // linkage to campaign if any
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; } // Amount allocated to generated codes
        public decimal RedeemedAmount { get; set; } // Amount already redeemed
        public int GeneratedCoupons { get; set; }
        public string Validity { get; set; } = string.Empty; // optional validity date/period
        public string CreatedAt { get; set; } = string.Empty;
    }
}
