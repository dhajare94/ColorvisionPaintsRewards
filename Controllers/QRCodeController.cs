using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Services;
using System.IO.Compression;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class QRCodeController : Controller
    {
        private readonly CodeService _codeService;
        private readonly CampaignService _campaignService;
        private readonly RewardSlabService _slabService;
        private readonly SettingsService _settingsService;
        private readonly QRCodeGeneratorService _qrService;
        private readonly ImgBBService _imgbbService;

        public QRCodeController(CodeService codeService, CampaignService campaignService,
            RewardSlabService slabService,
            SettingsService settingsService, QRCodeGeneratorService qrService, ImgBBService imgbbService)
        {
            _codeService = codeService;
            _campaignService = campaignService;
            _slabService = slabService;
            _settingsService = settingsService;
            _qrService = qrService;
            _imgbbService = imgbbService;
        }

        public async Task<IActionResult> Index()
        {
            var campaigns = await _campaignService.GetAllAsync();
            ViewBag.Campaigns = campaigns;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(string campaignId, int count, string batchName, string budgetId, string distribution)
        {
            var budget = await _slabService.GetByIdAsync(budgetId);
            if (budget == null) {
                TempData["Error"] = "Reward Template not found.";
                return RedirectToAction("Index", "Codes");
            }

            // In the new concept, TotalBudget is the per-batch budget.
            decimal targetBudget = budget.TotalBudget;
            
            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            var batchId = $"BATCH-{DateTime.UtcNow:yyyyMMddHHmmss}";

            // 1. Parse Allowed Values
            var allowedValues = (budget.AllowedRewardValues ?? "0,2,5,10,20,50")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => decimal.TryParse(v.Trim(), out var d) ? d : -1)
                .Where(d => d >= 0)
                .OrderBy(v => v).ToList();

            if (!budget.ZeroRewardAllowed) allowedValues.RemoveAll(v => v == 0);
            if (!allowedValues.Any()) allowedValues.Add(2m); // Minimum fallback

            // 2. Parse Weights
            var rawWeights = (budget.RewardWeights ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => double.TryParse(v.Trim(), out var w) ? w : 1.0)
                .ToList();
            
            // Normalize weights to match allowedValues count
            var finalWeights = new List<double>();
            for (int i = 0; i < allowedValues.Count; i++) {
                finalWeights.Add(i < rawWeights.Count ? rawWeights[i] : 1.0);
            }

            // 3. True Curiosity-Based Weighted Randomness Algorithm
            var amounts = new List<decimal>();
            Random rnd = new Random();

            // Safety check: Is batch budget even possible?
            decimal minPossible = allowedValues.Min() * count;
            decimal maxPossible = allowedValues.Max() * count;
            
            if (targetBudget < minPossible || targetBudget > maxPossible) {
                TempData["Error"] = $"Budget ₹{targetBudget} is impossible for {count} coupons with allowed rewards (Min: ₹{minPossible}, Max: ₹{maxPossible}).";
                return RedirectToAction("Index", "Codes");
            }

            int highRewardCount = 0;
            decimal actualMaxAllowedValue = allowedValues.Max();
            decimal currentRemainingBudget = targetBudget;

            // Generate rewards one by one, ensuring guaranteed reachability of the budget at every step.
            for (int i = 0; i < count; i++) {
                int remainingCoupons = count - i - 1;
                
                var currentAllowed = new List<decimal>(allowedValues);
                if (budget.MaxHighRewardCount > 0 && highRewardCount >= budget.MaxHighRewardCount) {
                    currentAllowed.Remove(actualMaxAllowedValue);
                }
                
                if (!currentAllowed.Any()) currentAllowed = new List<decimal>(allowedValues);

                decimal currentMinVal = currentAllowed.Min();
                decimal currentMaxVal = currentAllowed.Max();
                
                var validValues = new List<decimal>();
                var validWeights = new List<double>();
                
                for (int vIdx = 0; vIdx < currentAllowed.Count; vIdx++) {
                    decimal v = currentAllowed[vIdx];
                    double w = finalWeights[allowedValues.IndexOf(v)];
                    
                    decimal budgetAfter = currentRemainingBudget - v;
                    decimal minPossibleAfter = remainingCoupons * currentMinVal;
                    
                    decimal trueMaxPossibleAfter = 0;
                    if (budget.MaxHighRewardCount > 0 && currentMaxVal == actualMaxAllowedValue) {
                        int allowedHighsLeft = budget.MaxHighRewardCount - highRewardCount - (v == actualMaxAllowedValue ? 1 : 0);
                        int highsToUse = Math.Max(0, Math.Min(remainingCoupons, allowedHighsLeft));
                        int othersToUse = remainingCoupons - highsToUse;
                        decimal sndMax = currentAllowed.Where(x => x < actualMaxAllowedValue).DefaultIfEmpty(currentMinVal).Max();
                        trueMaxPossibleAfter = (highsToUse * actualMaxAllowedValue) + (othersToUse * sndMax);
                    } else {
                        trueMaxPossibleAfter = remainingCoupons * currentMaxVal;
                    }
                    
                    if (budgetAfter >= minPossibleAfter && budgetAfter <= trueMaxPossibleAfter) {
                        validValues.Add(v);
                        validWeights.Add(w);
                    }
                }
                
                if (validValues.Count == 0) {
                    // Absolute fallback if no perfectly safe value found (due to combination 'holes')
                    decimal midPoint = remainingCoupons > 0 ? currentRemainingBudget / remainingCoupons : currentRemainingBudget;
                    decimal closest = currentAllowed.OrderBy(v => Math.Abs(v - midPoint)).First();
                    validValues.Add(closest);
                    validWeights.Add(1.0);
                }
                
                double totalWeight = validWeights.Sum();
                double r = rnd.NextDouble() * totalWeight;
                double cumulative = 0;
                decimal selectedValue = validValues.Last(); // Default to last
                for (int j = 0; j < validValues.Count; j++) {
                    cumulative += validWeights[j];
                    if (r <= cumulative) {
                        selectedValue = validValues[j];
                        break;
                    }
                }
                
                amounts.Add(selectedValue);
                currentRemainingBudget -= selectedValue;
                if (selectedValue == actualMaxAllowedValue) {
                    highRewardCount++;
                }
            }

            // Final exact matching loop to guarantee ledger balances exactly if there are any tiny holes
            decimal currentSum = amounts.Sum();
            int failsafe = 0;
            while (currentSum != targetBudget && failsafe < count * 10)
            {
                failsafe++;
                int idx = rnd.Next(count);
                decimal oldVal = amounts[idx];
                
                if (currentSum < targetBudget) {
                    var possibleIncreases = allowedValues.Where(v => v > oldVal && currentSum + (v - oldVal) <= targetBudget).ToList();
                    if (possibleIncreases.Any()) {
                        decimal newVal = possibleIncreases[rnd.Next(possibleIncreases.Count)];
                        amounts[idx] = newVal;
                        currentSum += (newVal - oldVal);
                    }
                } else {
                    var possibleDecreases = allowedValues.Where(v => v < oldVal && currentSum - (oldVal - v) >= targetBudget).ToList();
                    if (possibleDecreases.Any()) {
                        decimal newVal = possibleDecreases[rnd.Next(possibleDecreases.Count)];
                        amounts[idx] = newVal;
                        currentSum -= (oldVal - newVal);
                    }
                }
            }

            if (currentSum != targetBudget) {
                // Force an exact match on a random element to guarantee accounting rule
                decimal diff = targetBudget - currentSum;
                int startIdx = rnd.Next(count);
                for (int i = 0; i < count; i++) {
                    int tryIdx = (startIdx + i) % count;
                    if (amounts[tryIdx] + diff >= 0) {
                         amounts[tryIdx] += diff;
                         break;
                    }
                }
            }

            // Final Shuffle for maximum curiosity and zero predictability
            int n = amounts.Count;  
            while (n > 1) {  
                n--;  
                int k = rnd.Next(n + 1);  
                decimal value = amounts[k];  
                amounts[k] = amounts[n];  
                amounts[n] = value;  
            }

            var generatedIds = await _codeService.GenerateCodesAsync(campaignId, count, baseUrl, batchId, batchName, budgetId, amounts);

            // Update budget tracker - Allocated is now lifetime total
            budget.AllocatedAmount += targetBudget;
            budget.GeneratedCoupons += count;
            await _slabService.UpdateAsync(budget.Id, budget);

            // Offload QR Code generation and ImgBB uploading to an async background task
            // This prevents the HTTP request from timing out on large batches (e.g. 100+ images)
            // Services used here are registered as Singletons so capturing them in Task.Run is safe.
            _ = Task.Run(async () => {
                foreach (var id in generatedIds)
                {
                    try {
                        var code = await _codeService.GetByIdAsync(id);
                        if (code != null)
                        {
                            var url = $"{baseUrl}?code={code.Code}";
                            var qrBytes = _qrService.GenerateQRCode(url, code.Code);
                            var imgbbUrl = await _imgbbService.UploadImageAsync(qrBytes, $"QR_{code.Code}");
                            
                            if (!string.IsNullOrEmpty(imgbbUrl))
                            {
                                await _codeService.UpdateQRUrlAsync(id, imgbbUrl);
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Background QR generation failed for ID {id}: {ex.Message}");
                    }
                    
                    // Add a tiny delay to help avoid hitting ImgBB or Firebase rate limits on large batches
                    await Task.Delay(100);
                }
            });

            TempData["Message"] = $"Successfully generated {generatedIds.Count} QR codes in the database. QR images and ImgBB uploads will process in the background.";
            TempData["GeneratedBatch"] = batchId;

            return RedirectToAction("Index", "Codes");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQR(string code)
        {
            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            var url = $"{baseUrl}?code={code}";
            var qrBytes = _qrService.GenerateQRCode(url, code);
            return File(qrBytes, "image/png", $"QR_{code}.png");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBatch(string batchId)
        {
            var codes = await _codeService.GetFilteredAsync(null, batchId, null);
            var settings = await _settingsService.GetSettingsAsync();
            var baseUrl = settings.BaseRedeemUrl;
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = $"{Request.Scheme}://{Request.Host}/Redeem";

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var code in codes)
                {
                    var url = $"{baseUrl}?code={code.Code}";
                    var qrBytes = _qrService.GenerateQRCode(url, code.Code);
                    var entry = archive.CreateEntry($"QR_{code.Code}.png");
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(qrBytes);
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", $"QRCodes_{batchId}.zip");
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchCodes(string batchId)
        {
            var codes = await _codeService.GetFilteredAsync(null, batchId, null);
            return Json(codes);
        }
    }
}
