using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public ShopController(HeriStepDbContext context)
        {
            _context = context;
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyShops([FromQuery] double userLat, [FromQuery] double userLon)
        {
            try
            {
                var stalls = await _context.Stalls
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && s.IsOpen)
                    .ToListAsync();

                var nearby = stalls.Select(s => new Shop
                {
                    Id = s.Id,
                    Name = s.Name,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    ImageUrl = string.IsNullOrEmpty(s.ImageUrl) 
                        ? "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80" 
                        : s.ImageUrl,
                    Distance = CalculateDistance(userLat, userLon, s.Latitude, s.Longitude)
                })
                .Where(s => s.Distance <= 15.0)
                .OrderBy(s => s.Distance)
                .ToList();

                return Ok(nearby);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching shops", details = ex.Message });
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; 
            var dLat = (lat2 - lat1) * (Math.PI / 180);
            var dLon = (lon2 - lon1) * (Math.PI / 180);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
