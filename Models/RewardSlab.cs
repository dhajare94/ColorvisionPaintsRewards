using System.Collections.Generic;

namespace QRRewardPlatform.Models
{
    public class RewardSlab // Treated as 'Budget' in UI and Logic now
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty; // linkage to campaign if any
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; } // Lifetime allocated amount across all batches
        public decimal RedeemedAmount { get; set; } // Amount already redeemed
        public int GeneratedCoupons { get; set; }
        public string AllowedRewardValues { get; set; } = "0,2,5,10,20,50"; // Comma-separated list
        public string RewardWeights { get; set; } = ""; // JSON or comma-separated relative weights
        public int MaxHighRewardCount { get; set; } = 0; // 0 means no limit
        public bool ZeroRewardAllowed { get; set; } = true;
        public bool IsReusable { get; set; } = true;
        public string RandomMode { get; set; } = "Curiosity"; // "Curiosity", "Equal", etc.
        public string Validity { get; set; } = string.Empty; // optional validity date/period
        public string CreatedAt { get; set; } = string.Empty;
    }
}
