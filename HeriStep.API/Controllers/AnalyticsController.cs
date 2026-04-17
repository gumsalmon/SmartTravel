using HeriStep.API.Data;
using HeriStep.Shared.Models;
using HeriStep.Shared.Models.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public AnalyticsController(HeriStepDbContext context)
        {
            _context = context;
        }

        [HttpPost("trajectory")]
        public async Task<IActionResult> SyncMobileTrackingData([FromBody] SyncTrackingPayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.DeviceId))
            {
                return BadRequest(new { Message = "Dữ liệu hoặc DeviceId không hợp lệ." });
            }

            try
            {
                // 1. Lưu Location Logs (TouristTrajectories)
                if (payload.LocationLogs != null && payload.LocationLogs.Any())
                {
                    var trajectories = payload.LocationLogs.Select(log => new TouristTrajectory
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = payload.DeviceId,
                        Latitude = log.Lat,
                        Longitude = log.Lng,
                        RecordedAt = log.Timestamp
                    }).ToList();

                    await _context.TouristTrajectories.AddRangeAsync(trajectories);
                }

                // 2. Lưu Listen Logs (StallVisits)
                if (payload.ListenLogs != null && payload.ListenLogs.Any())
                {
                    var visits = payload.ListenLogs.Select(log => new StallVisit
                    {
                        Id = Guid.NewGuid(),
                        StallId = log.PoiId,
                        DeviceId = payload.DeviceId,
                        VisitedAt = DateTime.UtcNow,
                        CreatedAtServer = DateTime.UtcNow,
                        ListenDurationSeconds = log.ListenDurationSeconds
                    }).ToList();

                    await _context.StallVisits.AddRangeAsync(visits);
                }

                // Tiết kiệm connection db: Gộp thành 1 lần SaveChanges
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Đồng bộ dữ liệu tracking thành công." });
            }
            catch (Exception ex)
            {
                // Log exception in a real application
                return StatusCode(500, new { Message = "Lỗi khi lưu tracking data.", Error = ex.Message });
            }
        }

        [HttpGet("avg-listen-time")]
        public async Task<IActionResult> GetAvgListenTime()
        {
            try
            {
                var data = await _context.StallVisits
                    .AsNoTracking()
                    .Where(v => v.ListenDurationSeconds > 0)
                    .GroupBy(v => v.StallId)
                    .Select(g => new
                    {
                        StallId = g.Key,
                        AvgSeconds = g.Average(x => x.ListenDurationSeconds),
                        VisitCount = g.Count()
                    })
                    .Join(_context.Stalls.AsNoTracking(),
                        visit => visit.StallId,
                        stall => stall.Id,
                        (visit, stall) => new
                        {
                            stallName = stall.Name,
                            avgSeconds = visit.AvgSeconds,
                            visitCount = visit.VisitCount
                        })
                    .OrderByDescending(x => x.avgSeconds)
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi truy vấn dữ liệu.", Error = ex.Message });
            }
        }
    }
}
