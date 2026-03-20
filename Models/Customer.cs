namespace QRRewardPlatform.Models
{
    public class Customer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Contractor, Builder, Supervisor, Painter, Dealer, Other

        public string UpiNumber { get; set; } = string.Empty; // Optional Bank/UPI Details
        public string Rank { get; set; } = "Bronze Partner"; 
        public int TotalBagsRedeemed { get; set; } = 0;
        public decimal TotalRewards { get; set; } = 0;
        public string CreatedDate { get; set; } = string.Empty;
    }
}
