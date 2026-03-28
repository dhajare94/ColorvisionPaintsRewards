using QRRewardPlatform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QRRewardPlatform.Services
{
    public class EnquiryService
    {
        private readonly FirebaseService _firebase;
        private const string Node = "websiteEnquiries";
        private const string CounterNode = "counters/enquiries";

        public EnquiryService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<List<WebsiteEnquiry>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<WebsiteEnquiry>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).OrderByDescending(x => x.CreatedAt).ToList();
        }

        public async Task<WebsiteEnquiry?> GetByIdAsync(string id)
        {
            var item = await _firebase.GetByIdAsync<WebsiteEnquiry>(Node, id);
            if (item != null) item.Id = id;
            return item;
        }

        public async Task<string> CreateAsync(WebsiteEnquiry enquiry)
        {
            // 1. Get next numeric ID
            var nextId = await GetNextEnquiryIdAsync();
            enquiry.EnquiryId = nextId;

            // 2. Timestamps
            var now = DateTime.UtcNow.ToString("o");
            enquiry.CreatedAt = now;
            enquiry.UpdatedAt = now;

            // 3. Duplicate check
            enquiry.IsDuplicate = await CheckDuplicateAsync(enquiry.Mobile, enquiry.ProductInterested);

            // 4. Save
            return await _firebase.PushAsync(Node, enquiry);
        }

        public async Task UpdateAsync(string id, WebsiteEnquiry enquiry)
        {
            enquiry.UpdatedAt = DateTime.UtcNow.ToString("o");
            await _firebase.UpdateAsync(Node, id, enquiry);
        }

        public async Task UpdateStatusAsync(string id, string status, string? remarks = null)
        {
            var existing = await GetByIdAsync(id);
            if (existing != null)
            {
                existing.Status = status;
                if (!string.IsNullOrEmpty(remarks))
                {
                    var timestamp = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
                    var newRemarks = $"[{timestamp}] {remarks}";
                    existing.Remarks = string.IsNullOrEmpty(existing.Remarks) 
                        ? newRemarks 
                        : $"{existing.Remarks}\n{newRemarks}";
                }
                await UpdateAsync(id, existing);
            }
        }

        private async Task<long> GetNextEnquiryIdAsync()
        {
            var current = await _firebase.GetByIdAsync<long?>(CounterNode, "current") ?? 0;
            var next = current + 1;
            await _firebase.SetAsync(CounterNode, "current", next);
            return next;
        }

        public async Task<EnquiryAnalytics> GetAnalyticsAsync()
        {
            var all = await GetAllAsync();
            var today = DateTime.UtcNow.Date;

            var analytics = new EnquiryAnalytics
            {
                TotalEnquiries = all.Count,
                TodayEnquiries = all.Count(e => DateTime.TryParse(e.CreatedAt, out var dt) && dt.Date == today),
                TotalQuantity = all.Sum(e => e.Quantity),
                TopProducts = all.Where(e => !string.IsNullOrEmpty(e.ProductName))
                    .GroupBy(e => e.ProductName)
                    .Select(g => new ProductStat { ProductName = g.Key!, Count = g.Count(), TotalQuantity = g.Sum(e => e.Quantity) })
                    .OrderByDescending(p => p.Count)
                    .Take(5)
                    .ToList(),
                EnquiriesByRole = all.GroupBy(e => e.RoleType ?? "Other")
                    .ToDictionary(g => g.Key, g => g.Count()),
                ProductWiseSummary = all.Where(e => !string.IsNullOrEmpty(e.ProductName))
                    .GroupBy(e => e.ProductName)
                    .Select(g => new ProductStat { ProductName = g.Key!, Count = g.Count(), TotalQuantity = g.Sum(e => e.Quantity) })
                    .ToList()
            };

            return analytics;
        }

        private async Task<bool> CheckDuplicateAsync(string mobile, string? product)
        {
            var all = await GetAllAsync();
            var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

            return all.Any(e => 
                e.Mobile == mobile && 
                (e.ProductInterested == product || e.ProductName == product) && 
                DateTime.TryParse(e.CreatedAt, out var createdAt) && 
                createdAt > twentyFourHoursAgo);
        }
    }

    public class EnquiryAnalytics
    {
        public int TotalEnquiries { get; set; }
        public int TodayEnquiries { get; set; }
        public int TotalQuantity { get; set; }
        public List<ProductStat> TopProducts { get; set; } = new();
        public Dictionary<string, int> EnquiriesByRole { get; set; } = new();
        public List<ProductStat> ProductWiseSummary { get; set; } = new();
    }

    public class ProductStat
    {
        public string ProductName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQuantity { get; set; }
    }
}
