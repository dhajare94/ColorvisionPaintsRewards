namespace QRRewardPlatform.Models
{
    public class PayoutBatch
    {
        public string Id { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int UserCount { get; set; }
        public string CreatedDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending / Completed
        public string CompletedDate { get; set; } = string.Empty;
    }
}
