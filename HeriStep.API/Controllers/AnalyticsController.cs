using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnalyticsController : ControllerBase
{
    private readonly HeriStepDbContext _context;

    public AnalyticsController(HeriStepDbContext context)
    {
        _context = context;
    }

    [HttpPost("track")]
    public async Task<IActionResult> TrackAsync([FromBody] TrackRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return BadRequest(new { message = "deviceId is required." });
            }

            var item = new TouristTrajectory
            {
                DeviceId = request.DeviceId.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                RecordedAt = request.RecordedAt ?? DateTime.UtcNow
            };

            _context.TouristTrajectories.Add(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tracked", id = item.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS] track failed: {ex.Message}");
            return StatusCode(500, new { message = "Track failed", detail = ex.Message });
        }
    }

    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmapAsync()
    {
        try
        {
            var points = await _context.TouristTrajectories
                .AsNoTracking()
                .OrderBy(t => t.RecordedAt)
                .Select(t => new
                {
                    lat = t.Latitude,
                    lng = t.Longitude,
                    deviceId = t.DeviceId,
                    recordedAt = t.RecordedAt
                })
                .ToListAsync();

            return Ok(points);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS] heatmap failed: {ex.Message}");
            return StatusCode(500, new { message = "Heatmap query failed", detail = ex.Message });
        }
    }

    [HttpGet("route/{deviceId}")]
    public async Task<IActionResult> GetRouteAsync(string deviceId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return BadRequest(new { message = "deviceId is required." });
            }

            var route = await _context.TouristTrajectories
                .AsNoTracking()
                .Where(t => t.DeviceId == deviceId)
                .OrderBy(t => t.RecordedAt)
                .Select(t => new
                {
                    lat = t.Latitude,
                    lng = t.Longitude,
                    recordedAt = t.RecordedAt
                })
                .ToListAsync();

            return Ok(route);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS] route failed: {ex.Message}");
            return StatusCode(500, new { message = "Route query failed", detail = ex.Message });
        }
    }

    [HttpGet("device-ids-recent")]
    public async Task<IActionResult> GetRecentDeviceIdsAsync([FromQuery] int take = 20)
    {
        try
        {
            var limited = Math.Clamp(take, 1, 200);
            var ids = await _context.TouristTrajectories
                .AsNoTracking()
                .Where(t => !string.IsNullOrWhiteSpace(t.DeviceId))
                .GroupBy(t => t.DeviceId)
                .Select(g => new
                {
                    deviceId = g.Key,
                    lastSeen = g.Max(x => x.RecordedAt)
                })
                .OrderByDescending(x => x.lastSeen)
                .Take(limited)
                .ToListAsync();

            return Ok(ids);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS] device-ids-recent failed: {ex.Message}");
            return StatusCode(500, new { message = "Cannot fetch recent device IDs", detail = ex.Message });
        }
    }

    [HttpGet("avg-listen-time")]
    public async Task<IActionResult> GetAverageListenTimeAsync()
    {
        try
        {
            var rows = await _context.StallVisits
                .AsNoTracking()
                .GroupBy(v => v.StallId)
                .Select(g => new
                {
                    StallId = g.Key,
                    AvgListenDurationSeconds = g.Average(x => (double)x.ListenDurationSeconds)
                })
                .Join(_context.Stalls.AsNoTracking(),
                    a => a.StallId,
                    s => s.Id,
                    (a, s) => new
                    {
                        stallId = a.StallId,
                        stallName = s.Name ?? "Unknown",
                        avgListenDurationSeconds = Math.Round(a.AvgListenDurationSeconds, 2)
                    })
                .OrderByDescending(x => x.avgListenDurationSeconds)
                .ToListAsync();

            return Ok(rows);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS] avg-listen-time failed: {ex.Message}");
            return StatusCode(500, new { message = "Average listen time query failed", detail = ex.Message });
        }
    }

    [HttpPost("stall-visit")]
    public async Task<IActionResult> SyncStallVisitAsync([FromBody] StallVisitSyncRequest request)
    {
        try
        {
            if (request == null || request.StallId <= 0)
            {
                return BadRequest(new { message = "Invalid stall visit payload." });
            }

            var visit = new StallVisit
            {
                Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
                StallId = request.StallId,
                DeviceId = request.DeviceId,
                VisitedAt = request.VisitedAt ?? DateTime.UtcNow,
                CreatedAtServer = DateTime.UtcNow,
                ListenDurationSeconds = Math.Max(0, request.ListenDurationSeconds)
            };

            _context.StallVisits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Stall visit synced", id = visit.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANALYTICS] stall-visit sync failed: {ex.Message}");
            return StatusCode(500, new { message = "Sync stall visit failed", detail = ex.Message });
        }
    }

    public class TrackRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime? RecordedAt { get; set; }
    }

    public class StallVisitSyncRequest
    {
        public Guid Id { get; set; }
        public int StallId { get; set; }
        public string? DeviceId { get; set; }
        public DateTime? VisitedAt { get; set; }
        public int ListenDurationSeconds { get; set; }
    }
}
