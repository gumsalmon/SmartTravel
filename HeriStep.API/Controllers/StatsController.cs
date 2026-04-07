using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public StatsController(HeriStepDbContext context) => _context = context;

        [HttpGet("system-stats")]
        public async Task<ActionResult<DashboardStats>> GetSystemStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                /* 1. Khởi tạo Object Stats */
                var stats = new DashboardStats();

                /* 2. Tính toán các con số tổng quát (💡 THÊM AsNoTracking ĐỂ TỐI ƯU RAM) */
                stats.TotalStalls = await _context.Stalls.AsNoTracking().CountAsync();
                stats.TotalStallOwners = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "StallOwner");
                stats.TotalTours = await _context.Tours.AsNoTracking().CountAsync(t => t.IsActive == true);

                var now = DateTime.Now;
                stats.ActiveDevices = await _context.Subscriptions.AsNoTracking()
                    .CountAsync(s => s.IsActive == true && (s.ExpiryDate == null || s.ExpiryDate > now));

                stats.TotalVisits = await _context.StallVisits.AsNoTracking().CountAsync();
                stats.TotalLanguages = await _context.Languages.AsNoTracking().CountAsync();

                /* 3. Trạng thái sạp (Biểu đồ Donut) */
                stats.OpenStalls = await _context.Stalls.AsNoTracking().CountAsync(s => s.IsOpen == true);
                stats.ClosedStalls = stats.TotalStalls - stats.OpenStalls;

                /* 4. Xử lý thời gian lọc */
                var end = endDate?.Date ?? DateTime.Today;
                var start = startDate?.Date ?? end.AddDays(-6);

                if (start > end)
                {
                    var temp = start;
                    start = end;
                    end = temp;
                }

                /* 5. Thống kê vé theo ngày (Biểu đồ Đường) */
                var ticketsInPeriod = await _context.TouristTickets.AsNoTracking()
                    .Where(t => t.CreatedAt >= start && t.CreatedAt < end.AddDays(1))
                    .Select(t => t.CreatedAt) // Chỉ select trường cần thiết để nhẹ mạng
                    .ToListAsync();

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    stats.ChartLabels.Add(date.ToString("dd/MM"));
                    stats.ChartData.Add(ticketsInPeriod.Count(t => t.Date == date.Date));
                }

                /* 6. Top 5 Sạp Hàng (💡 SỬ DỤNG LINQ JOIN ĐỂ EF CORE DỊCH RA SQL CHUẨN XÁC VÀ NHANH NHẤT) */
                var topStalls = await _context.StallVisits.AsNoTracking()
                    .GroupBy(v => v.StallId)
                    .Select(g => new { StallId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .Join(_context.Stalls.AsNoTracking(),
                          visit => visit.StallId,
                          stall => stall.Id,
                          (visit, stall) => new { Name = stall.Name, Count = visit.Count })
                    .ToListAsync();

                foreach (var item in topStalls)
                {
                    stats.TopStallNames.Add(item.Name ?? "Sạp ẩn");
                    stats.TopStallVisits.Add(item.Count);
                }

                /* 7. Doanh thu theo Gói vé (💡 SỬ DỤNG LINQ JOIN THAY VÌ SUB-QUERY) */
                var revenue = await _context.TouristTickets.AsNoTracking()
                    .GroupBy(t => t.PackageId)
                    .Select(g => new { PackageId = g.Key, Total = g.Sum(t => (double)t.AmountPaid) })
                    .Join(_context.TicketPackages.AsNoTracking(),
                          t => t.PackageId,
                          p => p.Id,
                          (t, p) => new { Name = p.PackageName, Total = t.Total })
                    .ToListAsync();

                foreach (var item in revenue)
                {
                    stats.RevenueLabels.Add(item.Name ?? "Gói lẻ");
                    stats.RevenueData.Add(item.Total);
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                // 💡 BẢO VỆ API: Trả về lỗi 500 đàng hoàng thay vì văng Exception sập Server
                return StatusCode(500, new { message = "Lỗi khi thống kê dữ liệu hệ thống", error = ex.Message });
            }
        }
    }
}