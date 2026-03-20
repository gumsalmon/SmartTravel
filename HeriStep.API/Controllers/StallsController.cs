using HeriStep.API.Data;
using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        // 💡 0. [MỚI THÊM] ADMIN: LẤY TẤT CẢ SẠP (HIỆN BẢNG)
        // ==========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetStalls()
        {
            // Sử dụng Left Join để sạp chưa có chủ vẫn hiện lên
            var query = from s in _context.Stalls
                        join u in _context.Users on s.OwnerId equals u.Id into userGroup
                        from user in userGroup.DefaultIfEmpty()
                        select new PointOfInterest
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Latitude = s.Latitude,
                            Longitude = s.Longitude,
                            RadiusMeter = s.RadiusMeter,
                            IsOpen = s.IsOpen,
                            ImageUrl = s.ImageUrl,
                            OwnerId = s.OwnerId,
                            OwnerName = user != null ? user.FullName : "Chưa có chủ",
                            UpdatedAt = s.UpdatedAt
                        };

            return await query.OrderByDescending(x => x.Id).ToListAsync();
        }

        // ==========================================
        // 1. LẤY CHI TIẾT 1 SẠP
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
                ownerId = stall.OwnerId,
                latitude = stall.Latitude,
                longitude = stall.Longitude,
                radiusMeter = stall.RadiusMeter,
                ttsScript = ttsContent != null ? ttsContent.TtsScript : ""
            });
        }

        // ==========================================
        // 2. CẬP NHẬT SẠP & TTS & UPLOAD ẢNH (Hỗ trợ Admin gán chủ)
        // ==========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStall(int id, [FromForm] UpdateStallRequest req)
        {
            if (id != req.Id) return BadRequest(new { message = "ID không khớp!" });

            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng!" });

            // Xử lý Upload ảnh
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

            // Cập nhật thông tin cơ bản
            stall.Name = req.Name;
            stall.IsOpen = req.IsOpen;
            stall.OwnerId = req.OwnerId; // 💡 Gán chủ sạp ở đây
            stall.RadiusMeter = req.RadiusMeter;
            stall.UpdatedAt = DateTime.Now;

            // Cập nhật nội dung TTS (Đa ngôn ngữ)
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
        // 💡 2.1 [MỚI THÊM] XÓA SẠP
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStall(int id)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound();

            // Xóa nội dung liên quan
            var contents = _context.StallContents.Where(c => c.StallId == id);
            _context.StallContents.RemoveRange(contents);

            _context.Stalls.Remove(stall);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa sạp hàng!" });
        }

        // ==========================================
        // 3. ADMIN: LẤY TOÀN BỘ SẠP CHO BẢN ĐỒ
        // ==========================================
        [HttpGet("admin-map")]
        public async Task<IActionResult> GetAllStallsForMap()
        {
            var stalls = await _context.Stalls
                .Select(s => new { id = s.Id, name = s.Name ?? "Sạp chưa đặt tên", lat = s.Latitude, lng = s.Longitude, ownerId = s.OwnerId, imageUrl = s.ImageUrl })
                .ToListAsync();
            return Ok(stalls);
        }

        // ==========================================
        // 4. ADMIN: GÁN CHỦ CHO SẠP (Assign Nhanh)
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
        // 5. ADMIN: TẠO SẠP TẠI VỊ TRÍ (Click bản đồ)
        // ==========================================
        [HttpPost("create-at-pos")]
        public async Task<IActionResult> CreateAtPos([FromBody] CreateStallPos req)
        {
            var newStall = new Stall
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
        // 🔐 8. ĐỔI MẬT KHẨU CHỦ SẠP
        // ==========================================
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = await _context.Users.FindAsync(req.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng!" });

            if (user.PasswordHash != req.OldPassword)
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });
            }

            user.PasswordHash = req.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        // ==========================================
        // 🧪 ADMIN: TẠO MOCK DATA TỔNG HỢP (FULL BẢNG)
        // ==========================================
        [HttpPost("generate-mock")]
        public async Task<IActionResult> GenerateMockData([FromBody] MockDataRequest req)
        {
            var rand = new Random();
            var now = DateTime.Now;

            try
            {
                // 1. TẠO TOURS (Lộ trình) - Vì Stalls cần TourID
                var tours = await _context.Tours.ToListAsync();
                if (!tours.Any())
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        var t = new Tour { TourName = $"Hành trình di sản {i}", Description = "Mô tả tour", IsActive = true };
                        _context.Tours.Add(t);
                    }
                    await _context.SaveChangesAsync();
                    tours = await _context.Tours.ToListAsync();
                }

                // 2. TẠO USERS (Chủ sạp)
                var createdUsers = new List<User>();
                for (int i = 0; i < req.UserCount; i++)
                {
                    var user = new User
                    {
                        Username = $"user_{Guid.NewGuid().ToString("N").Substring(0, 5)}",
                        PasswordHash = "123456",
                        FullName = $"Chủ Sạp {i + 1}",
                        Role = "StallOwner"
                    };
                    _context.Users.Add(user);
                    createdUsers.Add(user);
                }
                await _context.SaveChangesAsync();

            // 2. TẠO STALLS & SUBS
            for (int i = 0; i < req.StallCount; i++)
            {
                double randomLat = 10.7601 + (rand.NextDouble() * 0.006 - 0.003);
                double randomLng = 106.7025 + (rand.NextDouble() * 0.006 - 0.003);

                var stall = new Stall
                {
                    Name = $"Quán Ăn {rand.Next(100, 999)}",
                    Latitude = randomLat,
                    Longitude = randomLng,
                    IsOpen = true,
                    RadiusMeter = 15,
                    OwnerId = createdUsers.Count > 0 ? createdUsers[rand.Next(createdUsers.Count)].Id : null
                };
                _context.Stalls.Add(stall);

                var sub = new Subscription
                {
                    DeviceId = $"HS-DEV-{rand.Next(1000, 9999)}",
                    ActivationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    StartDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(30),
                    IsActive = true
                };
                _context.Subscriptions.Add(sub);
            }
            await _context.SaveChangesAsync();

                // 4. TẠO TICKET PACKAGES (Gói vé)
                var package = await _context.TicketPackages.FirstOrDefaultAsync();
                if (package == null)
                {
                    package = new TicketPackage { PackageName = "Vé Tuần Vĩnh Khánh", Price = 50000, DurationHours = 168, IsActive = true };
                    _context.TicketPackages.Add(package);
                    await _context.SaveChangesAsync();
                }

                // 5. TẠO VÉ & LƯỢT GHÉ (Rải đều 90 ngày qua)
                for (int i = 0; i < req.VisitCount; i++)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));

                    // Thêm vé
                    _context.TouristTickets.Add(new TouristTicket
                    {
                        TicketCode = $"TC-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                        DeviceId = $"DEV-{rand.Next(1000, 9999)}",
                        PackageId = package.Id,
                        AmountPaid = package.Price,
                        CreatedAt = randomDate,
                        ExpiryDate = randomDate.AddHours(package.DurationHours)
                    });

                    // Thêm lượt ghé sạp
                    if (createdStalls.Any())
                    {
                        _context.StallVisits.Add(new StallVisit
                        {
                            StallId = createdStalls[rand.Next(createdStalls.Count)].Id,
                            DeviceId = $"DEV-{rand.Next(1000, 9999)}",
                            VisitedAt = randomDate
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Mock data đã khớp 100% với cấu trúc DB VinhKhanhTour!" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi chi tiết nhất có thể
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { error = "Lỗi khi lưu DB", detail = msg });
            }
        }
        // --- DTOs ---
        public class ChangePasswordRequest { public int UserId { get; set; } public string OldPassword { get; set; } = ""; public string NewPassword { get; set; } = ""; }
        public class UpdateStallRequest { public int Id { get; set; } public string Name { get; set; } = ""; public bool IsOpen { get; set; } public int? OwnerId { get; set; } public int RadiusMeter { get; set; } public string? TtsScript { get; set; } public IFormFile? ImageFile { get; set; } }
        public class AssignStallRequest { public int StallId { get; set; } public int OwnerId { get; set; } public string NewStallName { get; set; } = ""; }
        public class CreateStallPos { public double Latitude { get; set; } public double Longitude { get; set; } }
        public class MockDataRequest { public int UserCount { get; set; } public int StallCount { get; set; } public int VisitCount { get; set; } }
    }
}