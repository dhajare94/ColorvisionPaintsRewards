using QRRewardPlatform.Models;
using CsvHelper;
using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace QRRewardPlatform.Services
{
    public class PayoutService
    {
        private readonly FirebaseService _firebase;
        private readonly RedemptionService _redemptionService;
        private const string Node = "payoutBatches";

        public PayoutService(FirebaseService firebase, RedemptionService redemptionService)
        {
            _firebase = firebase;
            _redemptionService = redemptionService;
        }

        public async Task<List<PayoutBatch>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<PayoutBatch>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<PayoutBatch?> GetByIdAsync(string id)
        {
            var item = await _firebase.GetByIdAsync<PayoutBatch>(Node, id);
            if (item != null) item.Id = id;
            return item;
        }

        public async Task<string> CreateBatchAsync()
        {
            var pending = await _redemptionService.GetPendingAsync();
            if (pending.Count == 0) return string.Empty;

            var batch = new PayoutBatch
            {
                TotalAmount = pending.Sum(p => p.RewardAmount),
                UserCount = pending.Count,
                CreatedDate = DateTime.UtcNow.ToString("o"),
                Status = "Pending"
            };

            var batchId = await _firebase.PushAsync(Node, batch);

            // Mark all pending redemptions as part of this batch
            foreach (var r in pending)
            {
                await _redemptionService.MarkPaidAsync(r.Id, batchId);
            }

            return batchId;
        }

        public async Task MarkCompletedAsync(string id)
        {
            var batch = await GetByIdAsync(id);
            if (batch != null)
            {
                batch.Status = "Completed";
                batch.CompletedDate = DateTime.UtcNow.ToString("o");
                await _firebase.SetAsync(Node, id, batch);
            }
        }

        public async Task<byte[]> ExportCsvAsync(string batchId)
        {
            var allRedemptions = await _redemptionService.GetAllAsync();
            var batchRedemptions = allRedemptions.Where(r => r.PayoutBatchId == batchId).ToList();

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteHeader<PayoutExportRow>();
            csv.NextRecord();

            foreach (var r in batchRedemptions)
            {
                csv.WriteRecord(new PayoutExportRow
                {
                    UserName = r.UserName,
                    MobileNumber = r.MobileNumber,
                    UpiNumber = r.UpiNumber,
                    RewardAmount = r.RewardAmount,
                    RedemptionDate = r.RedemptionDate
                });
                csv.NextRecord();
            }

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportExcelAsync(string batchId)
        {
            var allRedemptions = await _redemptionService.GetAllAsync();
            var batchRedemptions = allRedemptions.Where(r => r.PayoutBatchId == batchId).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Payouts");

            worksheet.Cell(1, 1).Value = "User Name";
            worksheet.Cell(1, 2).Value = "Mobile Number";
            worksheet.Cell(1, 3).Value = "UPI Number";
            worksheet.Cell(1, 4).Value = "Reward Amount";
            worksheet.Cell(1, 5).Value = "Redemption Date";

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
            headerRange.Style.Font.FontColor = XLColor.White;

            int row = 2;
            foreach (var r in batchRedemptions)
            {
                worksheet.Cell(row, 1).Value = r.UserName;
                worksheet.Cell(row, 2).Value = r.MobileNumber;
                worksheet.Cell(row, 3).Value = r.UpiNumber;
                worksheet.Cell(row, 4).Value = r.RewardAmount;
                worksheet.Cell(row, 5).Value = r.RedemptionDate;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }

    public class PayoutExportRow
    {
        public string UserName { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public string UpiNumber { get; set; } = "";
        public decimal RewardAmount { get; set; }
        public string RedemptionDate { get; set; } = "";
    }
}
