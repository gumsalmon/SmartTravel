using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public StatsController(HeriStepDbContext context) => _context = context;

        // ==========================================================
        // 1. DASHBOARD TỔNG QUAN: KPI Cards + Biểu đồ chính
        //    Admin Web gọi: GET /api/Stats?startDate=...&endDate=...
        // ==========================================================
        [HttpGet]
        public async Task<ActionResult<DashboardStats>> GetSystemStats(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var stats = new DashboardStats();

                /* 1. Các con số tổng quát */
                stats.TotalStalls = await _context.Stalls.AsNoTracking()
                    .CountAsync(s => s.IsDeleted == false);

                stats.TotalStallOwners = await _context.Users.AsNoTracking()
                    .CountAsync(u => u.Role == "StallOwner" && u.IsDeleted == false);

                stats.TotalTours = await _context.Tours.AsNoTracking()
                    .CountAsync(t => t.IsActive == true && t.IsDeleted == false);

                var now = DateTime.Now;
                stats.ActiveDevices = await _context.Subscriptions.AsNoTracking()
                    .CountAsync(s => s.IsActive == true && (s.ExpiryDate == null || s.ExpiryDate > now));

                stats.TotalVisits = await _context.StallVisits.AsNoTracking().CountAsync();

                stats.TotalLanguages = await _context.Languages.AsNoTracking()
                    .CountAsync(l => l.IsDeleted == false);

                /* 2. Trạng thái sạp (Biểu đồ Donut) */
                stats.OpenStalls = await _context.Stalls.AsNoTracking()
                    .CountAsync(s => s.IsOpen == true && s.IsDeleted == false);
                stats.ClosedStalls = stats.TotalStalls - stats.OpenStalls;

                /* 3. Xử lý thời gian lọc */
                var end   = endDate?.Date ?? DateTime.Today;
                var start = startDate?.Date ?? end.AddDays(-6);

                /* 4. Thống kê vé theo ngày (Biểu đồ Đường) */
                var ticketsInPeriod = await _context.TouristTickets.AsNoTracking()
                    .Where(t => t.CreatedAt >= start && t.CreatedAt < end.AddDays(1))
                    .Select(t => t.CreatedAt)
                    .ToListAsync();

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    stats.ChartLabels.Add(date.ToString("dd/MM"));
                    stats.ChartData.Add(ticketsInPeriod.Count(t => t.Date == date.Date));
                }

                /* 5. Top 5 Sạp theo lượt ghé thăm (VisitCount) */
                var topStalls = await _context.StallVisits.AsNoTracking()
                    .GroupBy(v => v.StallId)
                    .Select(g => new { StallId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .Join(_context.Stalls.AsNoTracking(),
                          v => v.StallId,
                          s => s.Id,
                          (v, s) => new { s.Name, v.Count })
                    .ToListAsync();

                foreach (var item in topStalls)
                {
                    stats.TopStallNames.Add(item.Name ?? "Sạp ẩn");
                    stats.TopStallVisits.Add(item.Count);
                }

                /* 6. Doanh thu theo Gói vé */
                var revenue = await _context.TouristTickets.AsNoTracking()
                    .GroupBy(t => t.PackageId)
                    .Select(g => new { PackageId = g.Key, Total = g.Sum(t => (double)t.AmountPaid) })
                    .Join(_context.TicketPackages.AsNoTracking(),
                          t => t.PackageId,
                          p => p.Id,
                          (t, p) => new { p.PackageName, Total = t.Total })
                    .ToListAsync();

                foreach (var item in revenue)
                {
                    stats.RevenueLabels.Add(item.PackageName ?? "Gói lẻ");
                    stats.RevenueData.Add(item.Total);
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi Backend", detail = ex.Message });
            }
        }

        // ==========================================================
        // 2. TOP 5 SẠP THEO TỔNG THỜI GIAN NGHE (cho Dashboard)
        //    Admin Web gọi: GET /api/Stats/top5-by-listen
        //    Trả về [{stallName, totalListenSeconds, visitCount}]
        // ==========================================================
        [HttpGet("top5-by-listen")]
        public async Task<IActionResult> GetTop5ByListenTime()
        {
            try
            {
                var data = await _context.StallVisits
                    .AsNoTracking()
                    .Where(v => v.ListenDurationSeconds > 0)
                    .GroupBy(v => v.StallId)
                    .Select(g => new
                    {
                        StallId            = g.Key,
                        TotalListenSeconds = g.Sum(x => x.ListenDurationSeconds),
                        VisitCount         = g.Count()
                    })
                    .OrderByDescending(x => x.TotalListenSeconds)
                    .Take(5)
                    .Join(_context.Stalls.AsNoTracking(),
                          v => v.StallId,
                          s => s.Id,
                          (v, s) => new
                          {
                              stallName          = s.Name ?? "Sạp ẩn",
                              totalListenSeconds = v.TotalListenSeconds,
                              visitCount         = v.VisitCount
                          })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi truy vấn Top 5", detail = ex.Message });
            }
        }
    }
}
