using System.Collections.Generic;

namespace QRRewardPlatform.Models
{
    public class RewardSlab
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "range"; // Kept for legacy compatibility if needed
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
