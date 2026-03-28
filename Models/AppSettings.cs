namespace QRRewardPlatform.Models
{
    public class AppSettings
    {
        public string FirebaseUrl { get; set; } = string.Empty;
        public string BaseRedeemUrl { get; set; } = string.Empty;
        public int DefaultCampaignDays { get; set; } = 30;
        public string RewardRoundingRule { get; set; } = "floor"; // floor, ceil, round
        public string InstagramUrl { get; set; } = string.Empty;
    }
}
