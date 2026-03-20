using QRRewardPlatform.Models;

namespace QRRewardPlatform.Services
{
    public class CodeService
    {
        private readonly FirebaseService _firebase;
        private const string Node = "codes";

        public CodeService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<List<RedemptionCode>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<RedemptionCode>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<List<RedemptionCode>> GetFilteredAsync(string? campaignId, string? batchId, string? status)
        {
            var all = await GetAllAsync();
            if (!string.IsNullOrEmpty(campaignId))
                all = all.Where(c => c.CampaignId == campaignId).ToList();
            if (!string.IsNullOrEmpty(batchId))
                all = all.Where(c => c.BatchId == batchId).ToList();
            if (!string.IsNullOrEmpty(status))
                all = all.Where(c => c.Status == status).ToList();
            return all;
        }

        public async Task<RedemptionCode?> GetByIdAsync(string id)
        {
            var item = await _firebase.GetByIdAsync<RedemptionCode>(Node, id);
            if (item != null) item.Id = id;
            return item;
        }

        public async Task<RedemptionCode?> GetByCodeAsync(string code)
        {
            var all = await _firebase.GetAllAsync<RedemptionCode>(Node);
            var entry = all.FirstOrDefault(c => c.Value.Code == code);
            if (entry.Value != null) entry.Value.Id = entry.Key;
            return entry.Value;
        }

        public async Task<List<string>> GenerateCodesAsync(string campaignId, int count, string baseRedeemUrl, string batchId, string batchName)
        {
            var generatedIds = new List<string>();

            // Temporarily creates codes. QRUrl will be updated by the controller after uploading to ImgBB
            for (int i = 0; i < count; i++)
            {
                var uniqueCode = Guid.NewGuid().ToString("N")[..12].ToUpper();
                var qrUrl = $"{baseRedeemUrl}?code={uniqueCode}"; // Default to local generation URL if ImgBB fails

                var codeEntry = new RedemptionCode
                {
                    Code = uniqueCode,
                    CampaignId = campaignId,
                    BatchId = batchId,
                    BatchName = batchName,
                    Status = "Unused",
                    CreatedDate = DateTime.UtcNow.ToString("o"),
                    QRUrl = qrUrl 
                };

                var id = await _firebase.PushAsync(Node, codeEntry);
                generatedIds.Add(id);
            }

            return generatedIds;
        }

        public async Task UpdateQRUrlAsync(string id, string imgbbUrl)
        {
            var code = await GetByIdAsync(id);
            if (code != null)
            {
                code.QRUrl = imgbbUrl;
                await _firebase.SetAsync(Node, id, code);
            }
        }

        public async Task MarkRedeemedAsync(string id, string redemptionId)
        {
            var code = await GetByIdAsync(id);
            if (code != null)
            {
                code.Status = "Redeemed";
                code.RedeemedAt = DateTime.UtcNow.ToString("o");
                code.RedeemedBy = redemptionId;
                await _firebase.SetAsync(Node, id, code);
            }
        }

        public async Task DeleteAsync(string id)
        {
            await _firebase.DeleteAsync(Node, id);
        }
    }
}
