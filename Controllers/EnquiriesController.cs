using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRRewardPlatform.Models;
using QRRewardPlatform.Services;
using System.Threading.Tasks;

namespace QRRewardPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [EnableCors("WebsitePolicy")]
    public class EnquiriesController : ControllerBase
    {
        private readonly EnquiryService _enquiryService;

        public EnquiriesController(EnquiryService enquiryService)
        {
            _enquiryService = enquiryService;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                message = "ERP enquiry API is working"
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] WebsiteEnquiryDto dto)
        {
            if (!ModelState.IsValid)
            {
                // Log validation errors for debugging
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                System.Diagnostics.Debug.WriteLine($"Validation failed for enquiry: {errors}");

                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var enquiry = new WebsiteEnquiry
            {
                Name = dto.Name,
                Mobile = dto.Mobile,
                Email = dto.Email,
                City = dto.City,
                State = dto.State,
                RoleType = dto.RoleType,
                ProductInterested = dto.ProductInterested ?? dto.ProductName,
                ProductName = dto.ProductName,
                ProductSlug = dto.ProductSlug,
                Quantity = dto.Quantity ?? 1,
                CartJson = dto.CartItems != null ? System.Text.Json.JsonSerializer.Serialize(dto.CartItems) : null,
                Message = dto.Message,
                Source = dto.Source,
                PageUrl = dto.PageUrl,
                CreatedFrom = dto.CreatedFrom ?? "Website",
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Status = "New"
            };

            var id = await _enquiryService.CreateAsync(enquiry);
            var savedEnquiry = await _enquiryService.GetByIdAsync(id);

            return Ok(new
            {
                success = true,
                message = "Enquiry saved successfully",
                enquiryId = savedEnquiry?.EnquiryId ?? 0
            });
        }
    }
}
