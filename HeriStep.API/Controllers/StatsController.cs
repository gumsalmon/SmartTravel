using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public StatsController(HeriStepDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<DashboardStats>> GetStats()
        {
            return new DashboardStats
            {
                // 1. Đếm tổng số sạp hàng đang có trong hệ thống
                TotalStalls = await _context.Stalls.CountAsync(),

                // 2. Lọc và đếm số lượng tài khoản có quyền Chủ sạp
                TotalStallOwners = await _context.Users.CountAsync(u => u.Role == "StallOwner"),

                // 3. Đếm số Lộ trình (Tours) đang trong trạng thái Hoạt động
                TotalTours = await _context.Tours.CountAsync(t => t.IsActive == true),

                // 4. Đếm số lượng Gói cước / Thiết bị đang được kích hoạt
                ActiveDevices = await _context.Subscriptions.CountAsync(s => s.IsActive == true)
            };
        }
    }
}