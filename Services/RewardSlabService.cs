using QRRewardPlatform.Models;

namespace QRRewardPlatform.Services
{
    public class RewardSlabService
    {
        private readonly FirebaseService _firebase;
        private const string Node = "rewardSlabs";

        public RewardSlabService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<List<RewardSlab>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<RewardSlab>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<RewardSlab?> GetByIdAsync(string id)
        {
            var item = await _firebase.GetByIdAsync<RewardSlab>(Node, id);
            if (item != null) item.Id = id;
            return item;
        }

        public async Task<string> CreateAsync(RewardSlab slab)
        {
            slab.CreatedAt = DateTime.UtcNow.ToString("o");
            return await _firebase.PushAsync(Node, slab);
        }

        public async Task UpdateAsync(string id, RewardSlab slab)
        {
            await _firebase.SetAsync(Node, id, slab);
        }

        public async Task DeleteAsync(string id)
        {
            await _firebase.DeleteAsync(Node, id);
        }

        public decimal CalculateReward(RewardSlab slab)
        {
            var random = new Random();
            double min = (double)slab.MinAmount;
            double max = (double)slab.MaxAmount;
            
            if (min > max) 
                return (decimal)max;
                
            double reward = random.NextDouble() * (max - min) + min;
            return (decimal)Math.Round(reward, 2);
        }
    }
}
