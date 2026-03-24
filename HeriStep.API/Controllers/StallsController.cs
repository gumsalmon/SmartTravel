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
        // 💡 0. ADMIN: LẤY TẤT CẢ SẠP (HIỆN BẢNG)
        // ==========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetStalls()
        {
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
                .Where(c => c.StallId == id && c.LangCode == "vi")
                .FirstOrDefaultAsync();

            return Ok(new
            {
                id = stall.Id,
                name = stall.Name ?? "Sạp chưa đặt tên",
                imageUrl = stall.ImageUrl ?? "",
                isOpen = stall.IsOpen,
                ownerId = stall.OwnerId,
                latitude = stall.Latitude,
                longitude = stall.Longitude,
                radiusMeter = stall.RadiusMeter,
                ttsScript = ttsContent != null ? (ttsContent.TtsScript ?? "") : ""
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
            stall.OwnerId = req.OwnerId;
            stall.RadiusMeter = req.RadiusMeter;
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
        // 2.1 XÓA SẠP
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStall(int id)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound();

            var contents = _context.StallContents.Where(c => c.StallId == id);
            _context.StallContents.RemoveRange(contents);

            _context.Stalls.Remove(stall);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa sạp hàng!" });
        }

        // ==========================================
        // 💡 3. ADMIN: LẤY TOÀN BỘ SẠP CHO BẢN ĐỒ (ĐÃ FIX THÊM isExpired và isOpen)
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
                    ownerId = s.OwnerId,
                    imageUrl = s.ImageUrl,
                    isOpen = s.IsOpen,
                    // Sạp hết hạn nếu KHÔNG CÓ gói cước nào CÒN HẠN
                    isExpired = !_context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > DateTime.Now)
                })
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
            _context.Stalls.Add(new Stall { Name = "Sạp mới", Latitude = req.Latitude, Longitude = req.Longitude, IsOpen = true, RadiusMeter = 20 });
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
        // 8. ĐỔI MẬT KHẨU CHỦ SẠP
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
        // 9. MOCK TEST: GIẢ LẬP KHÁCH VÀO SẠP (TẠO DATA ĐỘNG)
        // ==========================================
        [HttpPost("{id}/simulate-tourist/{langCode}")]
        public async Task<IActionResult> SimulateTourist(int id, string langCode)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp" });

            string fakeDeviceId = $"MOCK-APP-{langCode.ToUpper()}-{Guid.NewGuid().ToString().Substring(0, 6)}";

            var visit = new StallVisit
            {
                StallId = id,
                DeviceId = fakeDeviceId,
                VisitedAt = DateTime.Now
            };

            _context.StallVisits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã lưu DB thành công",
                device = fakeDeviceId,
                lang = langCode
            });
        }

        // ==========================================
        // 🧪 ADMIN: TẠO MOCK DATA TỔNG HỢP
        // ==========================================
        [HttpPost("generate-mock")]
        public async Task<IActionResult> GenerateMockData([FromBody] MockDataRequest req)
        {
            var rand = new Random();
            var now = DateTime.Now;

            try
            {
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

                var createdStalls = new List<Stall>();
                for (int i = 0; i < req.StallCount; i++)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));

                    var stall = new Stall
                    {
                        Name = $"Sạp hàng {rand.Next(100, 999)}",
                        Latitude = 10.76 + (rand.NextDouble() * 0.01),
                        Longitude = 106.70 + (rand.NextDouble() * 0.01),
                        IsOpen = true,
                        RadiusMeter = 50,
                        OwnerId = createdUsers[rand.Next(createdUsers.Count)].Id,
                        TourID = tours[rand.Next(tours.Count)].Id
                    };
                    _context.Stalls.Add(stall);
                    createdStalls.Add(stall);

                    var sub = new Subscription
                    {
                        StallId = stall.Id, // Đã fix ánh xạ StallId
                        DeviceId = $"HS-DEV-{rand.Next(1000, 9999)}",
                        ActivationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        StartDate = randomDate,
                        ExpiryDate = randomDate.AddDays(30),
                        IsActive = true
                    };
                    _context.Subscriptions.Add(sub);
                }
                await _context.SaveChangesAsync();

                var package = await _context.TicketPackages.FirstOrDefaultAsync();
                if (package == null)
                {
                    package = new TicketPackage { PackageName = "Vé Tuần Vĩnh Khánh", Price = 50000, DurationHours = 168, IsActive = true };
                    _context.TicketPackages.Add(package);
                    await _context.SaveChangesAsync();
                }

                for (int i = 0; i < req.VisitCount; i++)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));

                    _context.TouristTickets.Add(new TouristTicket
                    {
                        TicketCode = $"TC-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                        DeviceId = $"DEV-{rand.Next(1000, 9999)}",
                        PackageId = package.Id,
                        AmountPaid = package.Price,
                        CreatedAt = randomDate,
                        ExpiryDate = randomDate.AddHours(package.DurationHours)
                    });

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
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { error = "Lỗi khi lưu DB", detail = msg });
            }
        }

        // ==========================================
        // 💡 10. THÊM MÓN ĂN MỚI (VÀ LƯU ẢNH LÊN SERVER)
        // ==========================================
        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromForm] int StallId, [FromForm] string Name, [FromForm] decimal BasePrice, IFormFile ImageFile)
        {
            try
            {
                string imageUrl = "";

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = DateTime.Now.Ticks.ToString() + "_" + ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    imageUrl = "/images/products/" + uniqueFileName;
                }

                var newProduct = new Product
                {
                    StallId = StallId,
                    BasePrice = BasePrice,
                    ImageUrl = imageUrl,
                    IsSignature = false
                };

                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();

                var translation = new ProductTranslation
                {
                    ProductId = newProduct.Id,
                    LangCode = "vi",
                    ProductName = Name
                };

                _context.ProductTranslations.Add(translation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Thêm món ăn thành công!", imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==========================================
        // 💡 11. LẤY DANH SÁCH THỰC ĐƠN CỦA 1 SẠP
        // ==========================================
        [HttpGet("{stallId}/products")]
        public async Task<IActionResult> GetProductsByStall(int stallId)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.StallId == stallId)
                    .Select(p => new
                    {
                        Id = p.Id,
                        BasePrice = p.BasePrice,
                        ImageUrl = p.ImageUrl,
                        Name = _context.ProductTranslations
                                .Where(t => t.ProductId == p.Id && t.LangCode == "vi")
                                .Select(t => t.ProductName)
                                .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tải thực đơn", detail = ex.Message });
            }
        }

        // ==========================================
        // 💡 12. GIA HẠN GÓI CƯỚC (NÚT THANH TOÁN WEB)
        // ==========================================
        [HttpPost("extend-subscription/{id}")]
        public async Task<IActionResult> ExtendSubscription(int id)
        {
            try
            {
                var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StallId == id);

                if (sub != null)
                {
                    // Nếu đã quá hạn thì tính từ hôm nay + 30 ngày. Nếu còn hạn thì cộng dồn.
                    if (sub.ExpiryDate < DateTime.Now || sub.ExpiryDate == null)
                        sub.ExpiryDate = DateTime.Now.AddDays(30);
                    else
                        sub.ExpiryDate = sub.ExpiryDate.Value.AddDays(30);

                    sub.IsActive = true;
                }
                else
                {
                    // Nếu sạp này chưa từng có gói cước, tạo mới 1 cái 
                    sub = new Subscription
                    {
                        StallId = id,
                        DeviceId = $"WEB-PAID-{id}",
                        ActivationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        StartDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(30),
                        IsActive = true
                    };
                    _context.Subscriptions.Add(sub);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Gia hạn thành công! Sạp đã được mở lại." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi gia hạn", detail = ex.Message });
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