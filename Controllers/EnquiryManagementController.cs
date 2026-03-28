using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;
using System.Linq;
using System.Threading.Tasks;

namespace QRRewardPlatform.Controllers
{
    [Authorize]
    public class EnquiryManagementController : Controller
    {
        private readonly EnquiryService _enquiryService;

        public EnquiryManagementController(EnquiryService enquiryService)
        {
            _enquiryService = enquiryService;
        }

        public async Task<IActionResult> Index(string? status, string? city, string? product, string? source, string? date)
        {
            var enquiries = await _enquiryService.GetAllAsync();
            var analytics = await _enquiryService.GetAnalyticsAsync();
            ViewBag.Analytics = analytics;

            // Apply filters
            if (!string.IsNullOrEmpty(status))
                enquiries = enquiries.Where(e => e.Status == status).ToList();
            
            if (!string.IsNullOrEmpty(city))
                enquiries = enquiries.Where(e => e.City?.Contains(city, System.StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (!string.IsNullOrEmpty(product))
                enquiries = enquiries.Where(e => e.ProductInterested?.Contains(product, System.StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (!string.IsNullOrEmpty(source))
                enquiries = enquiries.Where(e => e.Source?.Contains(source, System.StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (!string.IsNullOrEmpty(date))
            {
                if (System.DateTime.TryParse(date, out var filterDate))
                {
                    enquiries = enquiries.Where(e => 
                        System.DateTime.TryParse(e.CreatedAt, out var createdAt) && 
                        createdAt.Date == filterDate.Date).ToList();
                }
            }

            ViewBag.Status = status;
            ViewBag.City = city;
            ViewBag.Product = product;
            ViewBag.Source = source;
            ViewBag.Date = date;

            return View(enquiries);
        }

        public async Task<IActionResult> Details(string id)
        {
            var enquiry = await _enquiryService.GetByIdAsync(id);
            if (enquiry == null) return NotFound();

            return View(enquiry);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status, string remarks)
        {
            await _enquiryService.UpdateStatusAsync(id, status, remarks);
            TempData["Message"] = "Status updated successfully";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
