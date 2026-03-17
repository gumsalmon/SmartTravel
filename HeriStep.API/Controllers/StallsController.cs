using HeriStep.API.Data;
using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StallsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public StallsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. LẤY THÔNG TIN SẠP
        // ==========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStall(int id)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng này!" });

            var ttsContent = await _context.StallContents
                .FirstOrDefaultAsync(c => c.StallId == id && c.LangCode == "vi");

            return Ok(new
            {
                id = stall.Id,
                name = stall.Name,
                imageUrl = stall.ImageUrl,
                isOpen = stall.IsOpen,
                ttsScript = ttsContent != null ? ttsContent.TtsScript : ""
            });
        }

        // ==========================================
        // 2. CẬP NHẬT SẠP & TTS & UPLOAD ẢNH
        // ==========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStall(int id, [FromForm] UpdateStallRequest req)
        {
            if (id != req.Id) return BadRequest(new { message = "ID không khớp!" });

            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng!" });

            if (req.ImageFile != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(req.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await req.ImageFile.CopyToAsync(fileStream);
                }
                stall.ImageUrl = "/uploads/" + fileName;
            }

            stall.Name = req.Name;
            stall.IsOpen = req.IsOpen;
            stall.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrEmpty(req.TtsScript))
            {
                var oldContents = _context.StallContents.Where(c => c.StallId == id);
                _context.StallContents.RemoveRange(oldContents);

                _context.StallContents.Add(new StallContent { StallId = id, LangCode = "vi", TtsScript = req.TtsScript, IsActive = true });

                string[] foreignLangs = { "en", "ja", "ko", "zh", "fr", "es", "ru", "th", "de" };
                foreach (var lang in foreignLangs)
                {
                    _context.StallContents.Add(new StallContent
                    {
                        StallId = id,
                        LangCode = lang,
                        TtsScript = $"[AI TTS in {lang.ToUpper()}] {req.TtsScript}",
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin sạp thành công!" });
        }

        // ==========================================
        // 3. ADMIN: LẤY TOÀN BỘ SẠP
        // ==========================================
        [HttpGet("admin-map")]
        public async Task<IActionResult> GetAllStallsForMap()
        {
            var stalls = await _context.Stalls
                .Select(s => new { id = s.Id, name = s.Name ?? "Sạp chưa đặt tên", lat = s.Latitude, lng = s.Longitude, ownerId = s.OwnerId, imageUrl = s.ImageUrl})
                .ToListAsync();
            return Ok(stalls);
        }

        // ==========================================
        // 4. ADMIN: GÁN CHỦ CHO SẠP
        // ==========================================
        [HttpPut("assign")]
        public async Task<IActionResult> AssignStall([FromBody] AssignStallRequest req)
        {
            var stall = await _context.Stalls.FindAsync(req.StallId);
            if (stall == null) return NotFound(new { message = "Không tìm thấy tọa độ sạp này!" });

            stall.OwnerId = req.OwnerId;
            stall.Name = req.NewStallName;
            stall.IsOpen = true;
            stall.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gán sạp thành công!" });
        }

        // ==========================================
        // 5. ADMIN: TẠO SẠP TẠI VỊ TRÍ
        // ==========================================
        [HttpPost("create-at-pos")]
        public async Task<IActionResult> CreateAtPos([FromBody] CreateStallPos req)
        {
            _context.Stalls.Add(new PointOfInterest { Name = "Sạp mới", Latitude = req.Latitude, Longitude = req.Longitude, IsOpen = true, RadiusMeter = 20 });
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ==========================================
        // 6. ADMIN: LẤY DANH SÁCH CHỦ SẠP
        // ==========================================
        [HttpGet("available-owners")]
        public async Task<IActionResult> GetAvailableOwners()
        {
            var owners = await _context.Users
                .Select(u => new { id = u.Id, fullName = u.FullName ?? "Chưa cập nhật tên", username = u.Username })
                .ToListAsync();
            return Ok(owners);
        }

        // ==========================================
        // 7. LẤY TTS THEO NGÔN NGỮ
        // ==========================================
        [HttpGet("{id}/tts/{langCode}")]
        public async Task<IActionResult> GetStallTts(int id, string langCode)
        {
            var content = await _context.StallContents
                .FirstOrDefaultAsync(c => c.StallId == id && c.LangCode == langCode);
            if (content == null) return NotFound(new { message = "Không tìm thấy TTS" });
            return Ok(new { text = content.TtsScript });
        }

        // ==========================================
        // 🔐 MỚI THÊM: ĐỔI MẬT KHẨU CHỦ SẠP
        // ==========================================
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = await _context.Users.FindAsync(req.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng!" });

            // Kiểm tra mật khẩu cũ trực tiếp từ database
            if (user.PasswordHash != req.OldPassword)
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = req.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
    }

    // ==========================================
    // DTOs
    // ==========================================

    public class ChangePasswordRequest
    {
        public int UserId { get; set; }
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    public class UpdateStallRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsOpen { get; set; }
        public string? TtsScript { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    public class AssignStallRequest
    {
        public int StallId { get; set; }
        public int OwnerId { get; set; }
        public string NewStallName { get; set; } = "";
    }

    public class CreateStallPos
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}