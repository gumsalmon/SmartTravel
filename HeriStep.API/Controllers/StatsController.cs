using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")] // Đường dẫn sẽ là: api/Stats
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public StatsController(HeriStepDbContext context) => _context = context;

        [HttpGet] // Gọi trực tiếp api/Stats
        public async Task<ActionResult<DashboardStats>> GetSystemStats()
        {
            var stats = new DashboardStats
            {
                TotalStalls = await _context.Stalls.CountAsync(),
                TotalStallOwners = await _context.Users.CountAsync(u => u.Role == "StallOwner"),
                TotalTours = await _context.Tours.CountAsync(t => t.IsActive == true),
                ActiveDevices = await _context.Subscriptions
                    .CountAsync(s => s.IsActive == true && (s.ExpiryDate == null || s.ExpiryDate > DateTime.Now)),
                TotalVisits = await _context.StallVisits.CountAsync(),
                TotalLanguages = await _context.Languages.CountAsync()
            };

            return Ok(stats);
        }
    }
}