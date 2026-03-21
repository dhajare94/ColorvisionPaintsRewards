using QRRewardPlatform.Models;
using ZXing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QRRewardPlatform.Services
{
    public class RedemptionService
    {
        private readonly FirebaseService _firebase;
        private readonly CodeService _codeService;
        private readonly CampaignService _campaignService;
        private readonly RewardSlabService _slabService;
        private readonly CustomerService _customerService;
        private readonly RedemptionBatchService _batchService;
        private const string Node = "redemptions";

        public RedemptionService(FirebaseService firebase, CodeService codeService,
            CampaignService campaignService, RewardSlabService slabService,
            CustomerService customerService, RedemptionBatchService batchService)
        {
            _firebase = firebase;
            _codeService = codeService;
            _campaignService = campaignService;
            _slabService = slabService;
            _customerService = customerService;
            _batchService = batchService;
        }

        public async Task<List<Redemption>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<Redemption>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<List<Redemption>> GetFilteredAsync(string? campaignId, string? status)
        {
            var all = await GetAllAsync();
            if (!string.IsNullOrEmpty(campaignId))
                all = all.Where(r => r.CampaignId == campaignId).ToList();
            if (!string.IsNullOrEmpty(status))
                all = all.Where(r => r.PayoutStatus == status).ToList();
            return all;
        }

        public async Task<List<Redemption>> GetPendingAsync()
        {
            var all = await GetAllAsync();
            return all.Where(r => r.PayoutStatus == "Pending").ToList();
        }

        public async Task<(bool success, string message, decimal reward)> RedeemCodeAsync(
            string code, string userName, string mobileNumber, string city, string district, string category, string upiNumber)
        {
            var codeEntry = await _codeService.GetByCodeAsync(code);
            if (codeEntry == null) return (false, "Invalid code. This code does not exist.", 0);
            if (codeEntry.Status == "Redeemed") return (false, "This code has already been redeemed.", 0);

            var campaign = await _campaignService.GetByIdAsync(codeEntry.CampaignId);
            if (campaign == null || campaign.Status != "Active") return (false, "Campaign not found or inactive.", 0);

            var budget = await _slabService.GetByIdAsync(codeEntry.BudgetId ?? campaign.RewardSlabId);
            
            Customer customer = await _customerService.GetOrCreateAsync(mobileNumber, userName, city, district, category);

            if (!string.IsNullOrEmpty(upiNumber) && customer.UpiNumber != upiNumber)
            {
                customer.UpiNumber = upiNumber;
                await _customerService.UpdateAsync(customer.Id, customer);
            }
            
            string finalUpiNumber = !string.IsNullOrEmpty(upiNumber) ? upiNumber : (customer.UpiNumber ?? "");

            decimal rewardAmount = codeEntry.RewardAmount;

            var redemption = new Redemption
            {
                CodeId = codeEntry.Id,
                CampaignId = codeEntry.CampaignId,
                BudgetId = codeEntry.BudgetId,
                UserName = customer.Name,
                MobileNumber = customer.MobileNumber,
                City = customer.City,
                District = customer.District,
                Category = customer.Category,
                UpiNumber = finalUpiNumber,
                RewardAmount = rewardAmount,
                RedemptionDate = DateTime.UtcNow.ToString("o"),
                PayoutStatus = "Pending"
            };

            var redemptionId = await _firebase.PushAsync(Node, redemption);
            await _codeService.MarkRedeemedAsync(codeEntry.Id, redemptionId);
            
            if (budget != null) {
                budget.RedeemedAmount += rewardAmount;
                await _slabService.UpdateAsync(budget.Id, budget);
            }

            await _customerService.UpdateMetricsAsync(customer.Id, 1, rewardAmount);



            return (true, $"Congratulations! Your reward of ₹{rewardAmount:F2} has been recorded.", rewardAmount);
        }

        public async Task<(bool success, string message, decimal totalReward)> RedeemBatchAsync(
            List<IFormFile> files, string userName, string mobileNumber, string city, string district, string category, string upiNumber, List<string>? manualCodes = null)
        {
            if (files == null || files.Count == 0) return (false, "No images uploaded.", 0);

            Customer customer = await _customerService.GetOrCreateAsync(mobileNumber, userName, city, district, category);

            if (!string.IsNullOrEmpty(upiNumber) && customer.UpiNumber != upiNumber)
            {
                customer.UpiNumber = upiNumber;
                await _customerService.UpdateAsync(customer.Id, customer);
            }
            
            string finalUpiNumber = !string.IsNullOrEmpty(upiNumber) ? upiNumber : (customer.UpiNumber ?? "");

            var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions { TryHarder = true }
            };

            List<string> extractedCodes = manualCodes != null ? new List<string>(manualCodes) : new List<string>();
            
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            try
                            {
                                using (var image = Image.Load<Rgba32>(stream))
                                {
                                    var result = reader.Decode(image);
                                    if (result != null && !string.IsNullOrEmpty(result.Text))
                                    {
                                        if (!extractedCodes.Contains(result.Text))
                                        {
                                            extractedCodes.Add(result.Text);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Skip images that cannot be decoded or aren't valid images
                                Console.WriteLine($"Error decoding image: {ex.Message}");
                            }
                        }
                    }
                }
            }

            if (extractedCodes.Count == 0) return (false, "No valid QR codes found in the uploaded images.", 0);

            decimal totalReward = 0;
            int validCodes = 0;
            var redemptionIds = new List<string>();

            var batch = new RedemptionBatch
            {
                CustomerId = customer.Id,

                MobileNumber = customer.MobileNumber,
                Status = "PendingCreation"
            };

            var batchId = await _batchService.CreateAsync(batch); // Temporary creation to get ID

            foreach (var codeText in extractedCodes)
            {
                // We parse the exact QR code from the URL or text if it's the full URL.
                // Assuming the QR contains just the code, or a URL ending in ?code=XYZ
                string actualCode = codeText;
                if (actualCode.Contains("?code="))
                {
                    actualCode = actualCode.Substring(actualCode.IndexOf("?code=") + 6);
                }

                var codeEntry = await _codeService.GetByCodeAsync(actualCode);
                if (codeEntry != null && codeEntry.Status == "Unused")
                {
                    var campaign = await _campaignService.GetByIdAsync(codeEntry.CampaignId);
                    if (campaign != null && campaign.Status == "Active")
                    {
                        var budget = await _slabService.GetByIdAsync(codeEntry.BudgetId ?? campaign.RewardSlabId);
                        
                        decimal rewardAmount = codeEntry.RewardAmount;
                        totalReward += rewardAmount;
                        validCodes++;

                            var redemption = new Redemption
                            {
                                CodeId = codeEntry.Id,
                                CampaignId = codeEntry.CampaignId,
                                BudgetId = codeEntry.BudgetId,
                                UserName = customer.Name,
                                MobileNumber = customer.MobileNumber,
                                City = customer.City,
                                District = customer.District,
                                Category = customer.Category,
                                UpiNumber = finalUpiNumber,
                                RewardAmount = rewardAmount,
                                RedemptionDate = DateTime.UtcNow.ToString("o"),
                                PayoutStatus = "Pending",
                                RedemptionBatchId = batchId
                            };

                            var redId = await _firebase.PushAsync(Node, redemption);
                            redemptionIds.Add(redId);
                            await _codeService.MarkRedeemedAsync(codeEntry.Id, redId);


                            if (budget != null) {
                                budget.RedeemedAmount += rewardAmount;
                                await _slabService.UpdateAsync(budget.Id, budget);
                            }
                        }
                    }
                }


            if (validCodes == 0)
            {
                // Delete empty batch
                await _firebase.DeleteAsync("redemptionBatches", batchId);
                return (false, "QR codes were scanned but none were valid or unused.", 0);
            }

            // Update batch details
            batch.Id = batchId;
            batch.TotalCodesRedeemed = validCodes;
            batch.TotalRewardValue = totalReward;
            batch.RedemptionIds = redemptionIds;

            // Re-saving through service to trigger approval logic and metric updates
            await _firebase.DeleteAsync("redemptionBatches", batchId); // We delete the temporary open status and pass to CreateAsync to handle metrics logic properly
            batch.Id = await _batchService.CreateAsync(batch); // CreateAsync calculates metrics and determines approval

            // Update individual redemptions with new batch ID just in case
            foreach (var rid in redemptionIds)
            {
                var r = await _firebase.GetByIdAsync<Redemption>(Node, rid);
                if (r != null)
                {
                    r.RedemptionBatchId = batch.Id;
                    await _firebase.UpdateAsync(Node, rid, r);
                }
            }

            string successMsg = $"Successfully processed {validCodes} valid QR codes out of {extractedCodes.Count} scanned. Total Reward: ₹{totalReward:F2}.";


            return (true, successMsg, totalReward);
        }

        public async Task MarkPaidAsync(string id, string batchId)
        {
            var redemption = await _firebase.GetByIdAsync<Redemption>(Node, id);
            if (redemption != null)
            {
                redemption.Id = id;
                redemption.PayoutStatus = "Paid";
                redemption.PayoutBatchId = batchId;
                await _firebase.SetAsync(Node, id, redemption);
            }
        }
    }
}
