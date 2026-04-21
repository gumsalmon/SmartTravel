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

        // ==========================================================
        // 1. MOBILE APP GỬI DỮ LIỆU TRACKING LÊN (OFFLINE SYNC)
        // ==========================================================
        [HttpPost("trajectory")]
        public async Task<IActionResult> SyncMobileTrackingData([FromBody] SyncTrackingPayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.DeviceId))
                return BadRequest(new { Message = "Dữ liệu hoặc DeviceId không hợp lệ." });

            try
            {
                // 1. Lưu Location Logs → TouristTrajectories
                if (payload.LocationLogs != null && payload.LocationLogs.Any())
                {
                    var trajectories = payload.LocationLogs.Select(log => new TouristTrajectory
                    {
                        DeviceId = payload.DeviceId,
                        Latitude  = log.Lat,
                        Longitude = log.Lng,
                        RecordedAt = log.Timestamp
                    }).ToList();

                    await _context.TouristTrajectories.AddRangeAsync(trajectories);
                }

                // 2. Lưu Listen Logs → StallVisits
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

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Đồng bộ dữ liệu tracking thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lưu tracking data.", Error = ex.Message });
            }
        }

        // ==========================================================
        // 2. ADMIN - HEATMAP: Lấy tối đa 2000 điểm tọa độ gần nhất
        //    AdminMap.cshtml gọi: GET /api/analytics/heatmap
        //    Trả về mảng [{lat, lng}] để Leaflet.heat render
        // ==========================================================
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmapData([FromQuery] int take = 2000)
        {
            try
            {
                var data = await _context.TouristTrajectories
                    .AsNoTracking()
                    .OrderByDescending(t => t.RecordedAt)
                    .Take(take)
                    .Select(t => new { lat = t.Latitude, lng = t.Longitude })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi truy vấn heatmap.", Error = ex.Message });
            }
        }

        // ==========================================================
        // 3. ADMIN - ROUTE: Lấy toàn bộ tuyến đi của 1 Device cụ thể
        //    AdminMap.cshtml gọi: GET /api/analytics/route/{deviceId}
        //    Trả về mảng [{lat, lng}] sắp xếp theo thời gian
        // ==========================================================
        [HttpGet("route/{deviceId}")]
        public async Task<IActionResult> GetRouteByDevice(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BadRequest(new { Message = "DeviceId không hợp lệ." });

            try
            {
                var data = await _context.TouristTrajectories
                    .AsNoTracking()
                    .Where(t => t.DeviceId == deviceId)
                    .OrderBy(t => t.RecordedAt)
                    .Select(t => new { lat = t.Latitude, lng = t.Longitude })
                    .ToListAsync();

                if (!data.Any())
                    return NotFound(new { Message = $"Không tìm thấy dữ liệu GPS cho device '{deviceId}'." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy tuyến đi.", Error = ex.Message });
            }
        }

        // ==========================================================
        // 4. ADMIN - DEVICE IDs GẦN ĐÂY: Dropdown chọn device để vẽ route
        //    AdminMap.cshtml gọi: GET /api/analytics/device-ids-recent?take=30
        //    Trả về [{deviceId, lastSeen}]
        // ==========================================================
        [HttpGet("device-ids-recent")]
        public async Task<IActionResult> GetRecentDeviceIds([FromQuery] int take = 30)
        {
            try
            {
                var data = await _context.TouristTrajectories
                    .AsNoTracking()
                    .GroupBy(t => t.DeviceId)
                    .Select(g => new
                    {
                        deviceId = g.Key,
                        lastSeen = g.Max(t => t.RecordedAt).ToString("dd/MM HH:mm")
                    })
                    .OrderByDescending(x => x.lastSeen)
                    .Take(take)
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách Device ID.", Error = ex.Message });
            }
        }

        // ==========================================================
        // 5. ADMIN - AVG LISTEN TIME: Thời gian nghe trung bình mỗi sạp
        //    Analytics Page gọi: GET /api/analytics/avg-listen-time
        // ==========================================================
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
                        StallId    = g.Key,
                        AvgSeconds = g.Average(x => x.ListenDurationSeconds),
                        VisitCount = g.Count()
                    })
                    .Join(_context.Stalls.AsNoTracking(),
                        visit => visit.StallId,
                        stall => stall.Id,
                        (visit, stall) => new
                        {
                            stallName  = stall.Name,
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
    