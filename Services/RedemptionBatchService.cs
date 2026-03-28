using QRRewardPlatform.Models;

namespace QRRewardPlatform.Services
{
    public class RedemptionBatchService
    {
        private readonly FirebaseService _firebase;
        private readonly CustomerService _customerService;
        private const string Node = "redemptionBatches";

        public RedemptionBatchService(FirebaseService firebase, CustomerService customerService)
        {
            _firebase = firebase;
            _customerService = customerService;
        }

        public async Task<List<RedemptionBatch>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<RedemptionBatch>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<RedemptionBatch?> GetByIdAsync(string id)
        {
            var batch = await _firebase.GetByIdAsync<RedemptionBatch>(Node, id);
            if (batch != null) batch.Id = id;
            return batch;
        }

        public async Task<string> CreateAsync(RedemptionBatch batch)
        {
            batch.CreatedDate = DateTime.UtcNow.ToString("o");
            batch.Status = "Approved";
            batch.ApprovedDate = batch.CreatedDate;
            
            // Update customer metrics
            await _customerService.UpdateMetricsAsync(batch.CustomerId, batch.TotalCodesRedeemed, batch.TotalRewardValue);
            
            return await _firebase.PushAsync(Node, batch);
        }

        public async Task ApproveBatchAsync(string batchId)
        {
            var batch = await GetByIdAsync(batchId);
            if (batch != null && batch.Status == "PendingApproval")
            {
                batch.Status = "Approved";
                batch.ApprovedDate = DateTime.UtcNow.ToString("o");
                await _firebase.UpdateAsync(Node, batchId, batch);

                // Update customer metrics after manual approval
                await _customerService.UpdateMetricsAsync(batch.CustomerId, batch.TotalCodesRedeemed, batch.TotalRewardValue);
            }
        }

        public async Task RejectBatchAsync(string batchId)
        {
            var batch = await GetByIdAsync(batchId);
            if (batch != null && batch.Status == "PendingApproval")
            {
                batch.Status = "Rejected";
                await _firebase.UpdateAsync(Node, batchId, batch);
            }
        }
    }
}
