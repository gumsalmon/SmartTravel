using HeriStep.API.Data;
using HeriStep.Shared;
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
    public class PointsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public PointsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // 1. LẤY DANH SÁCH (JOIN 3 BẢNG: Stalls, StallContents, Users)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stall>>> GetPoints()
        {
            var points = await (from s in _context.Stalls
                                    // Join lấy nội dung tiếng Việt
                                join c in _context.StallContents on s.Id equals c.StallId
                                where c.LangCode == "vi"

                                // 💡 THÊM ĐOẠN NÀY: LEFT JOIN với bảng Users để lấy tên Chủ sạp
                                join u in _context.Users on s.OwnerId equals u.Id into userGroup
                                from user in userGroup.DefaultIfEmpty()

                                select new Stall
                                {
                                    Id = s.Id,
                                    OwnerId = s.OwnerId,                               // Lấy mã Chủ sạp
                                    OwnerName = user != null ? user.FullName : null,   // Lấy tên Chủ sạp
                                    Name = s.Name,
                                    Latitude = s.Latitude,
                                    Longitude = s.Longitude,
                                    RadiusMeter = s.RadiusMeter,
                                    ImageUrl = s.ImageUrl,
                                    IsOpen = s.IsOpen,
                                    UpdatedAt = s.UpdatedAt,
                                    TourID = s.TourID,
                                    TtsScript = c.TtsScript
                                }).ToListAsync();

            return points;
        }

        // 2. THÊM MỚI (LƯU VÀO HAI BẢNG)
        [HttpPost]
        public async Task<ActionResult<Stall>> PostPoint(Stall point)
        {
            // BƯỚC A: Lưu thông tin kỹ thuật vào bảng Stalls
            _context.Stalls.Add(point);
            await _context.SaveChangesAsync();

            // BƯỚC B: Lưu nội dung thuyết minh vào bảng StallContents
            var content = new StallContent
            {
                StallId = point.Id,
                LangCode = "vi",
                TtsScript = point.TtsScript ?? "",
                IsActive = true
            };
            _context.StallContents.Add(content);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPoints), new { id = point.Id }, point);

        }
        // THÊM VÀO PointsController.cs
        // API: Lấy danh sách sạp theo Mã Chủ Sạp
        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<IEnumerable<Stall>>> GetStallsByOwner(int ownerId)
        {
            var stalls = await _context.Stalls
.Where(s => (int?)s.OwnerId == ownerId).ToListAsync();

            if (!stalls.Any())
            {
                return NotFound(new { message = "Bạn chưa có sạp hàng nào quản lí , hãy liên hệ admin để được hỗ trợ" });
            }

            return Ok(stalls);
        }
    }
}