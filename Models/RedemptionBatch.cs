namespace QRRewardPlatform.Models
{
    public class RedemptionBatch
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;       // Link to the newly created/existing customer

        public string MobileNumber { get; set; } = string.Empty;     // Used as fallback search
        public decimal TotalRewardValue { get; set; } = 0;
        public int TotalCodesRedeemed { get; set; } = 0;
        public List<string> RedemptionIds { get; set; } = new List<string>(); // Individual code redemption IDs
        public string Status { get; set; } = "PendingApproval";      // PendingApproval, Approved, Rejected (Admin handles large batches)
        public string CreatedDate { get; set; } = string.Empty;
        public string ApprovedDate { get; set; } = string.Empty;
        public string PayoutStatus { get; set; } = "Pending";        // Will group them together when payout is processed
    }
}
