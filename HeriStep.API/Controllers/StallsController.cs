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
    public class StallsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public StallsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. LẤY THÔNG TIN SẠP (Đổ dữ liệu lên form cho Chủ Sạp)
        // ==========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<PointOfInterest>> GetStall(int id)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null)
            {
                return NotFound(new { message = "Không tìm thấy sạp hàng này!" });
            }
            return Ok(stall);
        }

        // ==========================================
        // 2. CẬP NHẬT SẠP & LỜI CHÀO TTS (Cho Chủ Sạp)
        // ==========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStall(int id, [FromBody] UpdateStallRequest req)
        {
            if (id != req.Id) return BadRequest("ID không khớp!");

            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng!" });

            stall.Name = req.Name;
            stall.ImageUrl = req.ImageUrl;
            stall.IsOpen = req.IsOpen;
            stall.TtsScript = req.TtsScript;
            stall.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin sạp thành công!" });
        }

        // ==========================================
        // 3. ADMIN: LẤY TOÀN BỘ SẠP ĐỂ VẼ BẢN ĐỒ
        // ==========================================
        [HttpGet("admin-map")]
        public async Task<IActionResult> GetAllStallsForMap()
        {
            var stalls = await _context.Stalls
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name ?? "Sạp chưa đặt tên",
                    lat = s.Latitude,
                    lng = s.Longitude,
                    ownerId = s.OwnerId
                })
                .ToListAsync();

            return Ok(stalls);
        }

        // ==========================================
        // 4. ADMIN: GÁN CHỦ CHO SẠP TRỐNG
        // ==========================================
        [HttpPut("assign")]
        public async Task<IActionResult> AssignStall([FromBody] AssignStallRequest req)
        {
            var stall = await _context.Stalls.FindAsync(req.StallId);
            if (stall == null) return NotFound(new { message = "Không tìm thấy tọa độ sạp này!" });

            var user = await _context.Users.FindAsync(req.OwnerId);
            if (user == null) return BadRequest(new { message = "Tài khoản chủ sạp không tồn tại!" });

            stall.OwnerId = req.OwnerId;
            stall.Name = req.NewStallName;
            stall.IsOpen = true;
            stall.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gán sạp thành công!" });
        }

        // ==========================================
        // 5. ADMIN: CLICK BẢN ĐỒ ĐỂ TẠO SẠP TRỐNG
        // ==========================================
        [HttpPost("create-at-pos")]
        public async Task<IActionResult> CreateAtPos([FromBody] CreateStallPos req)
        {
            var newStall = new PointOfInterest
            {
                Name = "Sạp mới (Chưa gán)",
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                IsOpen = true,
                RadiusMeter = 20,
                OwnerId = null
            };

            _context.Stalls.Add(newStall);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ==========================================
        // 6. ADMIN: LẤY DANH SÁCH CHỦ SẠP (Để thả vào Dropdown)
        // ==========================================
        [HttpGet("available-owners")]
        public async Task<IActionResult> GetAvailableOwners()
        {
            var owners = await _context.Users
                .Select(u => new
                {
                    id = u.Id,
                    fullName = u.FullName ?? "Chưa cập nhật tên",
                    username = u.Username
                })
                .ToListAsync();

            return Ok(owners);
        }
    } // <-- ĐÂY LÀ DẤU NGOẶC ĐÓNG CỦA CLASS STALLSCONTROLLER

    // ==========================================
    // CÁC CLASSES DTO (Bắt buộc nằm ngoài Controller nhưng trong Namespace)
    // ==========================================
    public class UpdateStallRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public bool IsOpen { get; set; }
        public string? TtsScript { get; set; }
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