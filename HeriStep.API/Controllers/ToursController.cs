using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public ToursController(HeriStepDbContext context)
        {
            _context = context;
        }

        [HttpGet("top10")]
        public async Task<IActionResult> GetTop10Tours()
        {
            // Lấy 10 tours nổi bật nhất
            var topTours = await _context.Tours
                .OrderByDescending(t => t.Id)
                .Take(10)
                .Select(t => new {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = string.IsNullOrEmpty(t.Description) ? "Lộ trình ăn vặt quận 4 độc đáo" : t.Description,
                    Rating = 4.8,
                    StallsCount = 12,
                    Price = 50000,
                    ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600&auto=format&fit=crop"
                })
                .ToListAsync();
            
            return Ok(topTours);
        }

        [HttpGet]
        public async Task<IActionResult> GetTours()
        {
            var tours = await _context.Tours.ToListAsync();
            return Ok(tours);
        }
    }
}
