using QRRewardPlatform.Models;
using Firebase.Database.Query;

namespace QRRewardPlatform.Services
{
    public class SettingsService
    {
        private readonly FirebaseService _firebase;
        private const string Node = "settings";

        public SettingsService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            try
            {
                var settings = await _firebase.Client.Child(Node).OnceSingleAsync<AppSettings>();
                return settings ?? GetDefaultSettings();
            }
            catch
            {
                return GetDefaultSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            await _firebase.Client.Child(Node).PutAsync(settings);
        }

        private AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                FirebaseUrl = "https://cvspincentive-default-rtdb.firebaseio.com/",
                BaseRedeemUrl = "http://localhost:5000/Redeem",
                DefaultCampaignDays = 30,
                RewardRoundingRule = "floor",
                InstagramUrl = "https://instagram.com/yourpage"
            };
        }
    }
}
