using QRRewardPlatform.Models;

namespace QRRewardPlatform.Services
{
    public class CampaignService
    {
        private readonly FirebaseService _firebase;
        private const string Node = "campaigns";

        public CampaignService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<List<Campaign>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<Campaign>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<Campaign?> GetByIdAsync(string id)
        {
            var item = await _firebase.GetByIdAsync<Campaign>(Node, id);
            if (item != null) item.Id = id;
            return item;
        }

        public async Task<string> CreateAsync(Campaign campaign)
        {
            campaign.CreatedAt = DateTime.UtcNow.ToString("o");
            return await _firebase.PushAsync(Node, campaign);
        }

        public async Task UpdateAsync(string id, Campaign campaign)
        {
            await _firebase.SetAsync(Node, id, campaign);
        }

        public async Task DeleteAsync(string id)
        {
            await _firebase.DeleteAsync(Node, id);
        }

        public async Task SetStatusAsync(string id, string status)
        {
            var campaign = await GetByIdAsync(id);
            if (campaign != null)
            {
                campaign.Status = status;
                await _firebase.SetAsync(Node, id, campaign);
            }
        }
    }
}
