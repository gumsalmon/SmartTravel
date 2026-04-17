using HeriStep.API.Data;
using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListenController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public ListenController(HeriStepDbContext context)
        {
            _context = context;
        }

        // GET: api/Listen/info/{stallId}
        [HttpGet("info/{stallId}")]
        public async Task<IActionResult> GetStallListenInfo(int stallId)
        {
            var stall = await _context.Stalls
                .Where(s => s.Id == stallId && !s.IsDeleted && s.IsOpen)
                .Select(s => new { s.Name, s.ImageUrl })
                .FirstOrDefaultAsync();

            if (stall == null)
            {
                return NotFound(new { message = "Sạp không tồn tại hoặc đã đóng cửa." });
            }

            var availableLanguages = await _context.StallContents
                .Where(sc => sc.StallId == stallId && sc.IsActive == true && !sc.IsDeleted)
                .Join(_context.Languages,
                    sc => sc.LangCode,
                    lang => lang.LangCode,
                    (sc, lang) => new
                    {
                        LangCode = sc.LangCode,
                        LangName = lang.LangName,
                        FlagIconUrl = lang.FlagIconUrl,
                        TtsScript = sc.TtsScript
                    })
                .Where(x => !string.IsNullOrEmpty(x.TtsScript))
                .ToListAsync();

            return Ok(new
            {
                stallName = stall.Name,
                imageUrl = stall.ImageUrl,
                availableLanguages
            });
        }

        public class TrackVisitDto
        {
            public int StallId { get; set; }
            public string DeviceId { get; set; } = string.Empty;
            public int Duration { get; set; }
        }

        // POST: api/Listen/track-visit
        [HttpPost("track-visit")]
        public async Task<IActionResult> TrackVisit([FromBody] TrackVisitDto dto)
        {
            // Bỏ qua nếu thời gian nghe chưa tới 5 giây để tránh nhiễu dữ liệu báo cáo
            if (dto.Duration < 5)
            {
                return Ok(new { message = "Skipped tracking (duration < 5s)" });
            }

            var visit = new StallVisit
            {
                StallId = dto.StallId,
                DeviceId = dto.DeviceId,
                ListenDurationSeconds = dto.Duration,
                VisitedAt = DateTime.Now,
                CreatedAtServer = DateTime.Now
            };

            _context.StallVisits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tracked successfully" });
        }
    }
}
