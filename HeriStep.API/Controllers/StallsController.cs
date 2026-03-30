using HeriStep.Shared.Models.DTOs.Responses;
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
using System.Security.Claims;
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStall(int id, [FromForm] UpdateStallRequest req)
        {
            try
            {
                if (id != req.Id) return BadRequest(new { message = "ID không khớp!" });

                var stall = await _context.Stalls.FindAsync(id);
                if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng!" });

                if (req.ImageFile != null && req.ImageFile.Length > 0)
                {
                    if (req.ImageFile.Length > 5 * 1024 * 1024)
                        return BadRequest(new { message = "File ảnh không được vượt quá 5MB!" });

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

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

                if (req.TtsScript != null)
                {
                    var oldContents = await _context.StallContents.Where(c => c.StallId == id).ToListAsync();
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
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Lỗi khi cập nhật sạp", detail });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStall(int id)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound();

            var contents = await _context.StallContents.Where(c => c.StallId == id).ToListAsync();
            _context.StallContents.RemoveRange(contents);

            _context.Stalls.Remove(stall);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa sạp hàng!" });
        }

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
                    isExpired = !_context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > DateTime.Now)
                })
                .ToListAsync();
            return Ok(stalls);
        }

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

        [HttpPost("create-at-pos")]
        public async Task<IActionResult> CreateAtPos([FromBody] CreateStallPos req)
        {
            _context.Stalls.Add(new Stall { Name = "Sạp mới", Latitude = req.Latitude, Longitude = req.Longitude, IsOpen = false, RadiusMeter = 20 });
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("available-owners")]
        public async Task<IActionResult> GetAvailableOwners()
        {
            var owners = await _context.Users
                .Select(u => new { id = u.Id, fullName = u.FullName ?? "Chưa cập nhật tên", username = u.Username })
                .ToListAsync();
            return Ok(owners);
        }

        [HttpGet("{id}/tts/{langCode}")]
        public async Task<IActionResult> GetStallTts(int id, string langCode)
        {
            var content = await _context.StallContents
                .FirstOrDefaultAsync(c => c.StallId == id && c.LangCode == langCode);
            if (content == null) return NotFound(new { message = "Không tìm thấy TTS" });
            return Ok(new { text = content.TtsScript });
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromForm] int StallId, [FromForm] string Name, [FromForm] decimal BasePrice, IFormFile ImageFile)
        {
            try
            {
                string imageUrl = "";
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (ImageFile.Length > 5 * 1024 * 1024)
                        return BadRequest(new { message = "File ảnh không được vượt quá 5MB!" });

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles", "images", "products");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = DateTime.Now.Ticks.ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                    imageUrl = "/images/products/" + uniqueFileName;
                }

                var newProduct = new Product { StallId = StallId, BasePrice = BasePrice, ImageUrl = imageUrl, IsSignature = false };
                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();

                _context.ProductTranslations.Add(new ProductTranslation { ProductId = newProduct.Id, LangCode = "vi", ProductName = Name });
                await _context.SaveChangesAsync();

                return Ok(new { message = "Thêm món ăn thành công!", imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // 💡 ĐÃ FIX: Chống lỗi Open DataReader bằng ToListAsync() trước khi xóa
        [HttpDelete("delete-product/{productId}")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return NotFound("Không tìm thấy món ăn này!");

                var translations = await _context.ProductTranslations.Where(t => t.ProductId == productId).ToListAsync();
                if (translations.Any())
                {
                    _context.ProductTranslations.RemoveRange(translations);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã xóa món ăn thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{stallId}/products")]
        public async Task<IActionResult> GetProductsByStall(int stallId)
        {
            try
            {
                var products = await _context.Products.Where(p => p.StallId == stallId).Select(p => new
                {
                    Id = p.Id,
                    BasePrice = p.BasePrice,
                    ImageUrl = p.ImageUrl,
                    Name = _context.ProductTranslations.Where(t => t.ProductId == p.Id && t.LangCode == "vi").Select(t => t.ProductName).FirstOrDefault()
                }).ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tải thực đơn", detail = ex.Message });
            }
        }

        [HttpGet("my-stalls")]
        public async Task<IActionResult> GetMyStalls([FromQuery] int? ownerId)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int finalOwnerId = ownerId ?? (int.TryParse(claimId, out var id) ? id : 0);

            if (finalOwnerId == 0) return Unauthorized(new { message = "Không xác định được chủ sạp. Vui lòng đăng nhập lại!" });

            var stalls = await _context.Stalls.Where(s => s.OwnerId == finalOwnerId).ToListAsync();
            return Ok(stalls);
        }

        [HttpGet("top5")]
        public async Task<IActionResult> GetTop5Stalls()
        {
            var top = await _context.Stalls
                .Where(s => s.IsOpen)
                .OrderByDescending(s => s.RadiusMeter)
                .Take(5)
                .Select(s => new {
                    Id = s.Id,
                    Name = s.Name,
                    ImageUrl = string.IsNullOrEmpty(s.ImageUrl) ? "https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80" : s.ImageUrl,
                    Rating = 5.0,
                    ReviewCount = 100
                })
                .ToListAsync();
            return Ok(top);
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyStalls()
        {
            return await GetAllStallsForMap();
        }

        [HttpPost("extend-subscription/{id}")]
        public async Task<IActionResult> ExtendSubscription(int id)
        {
            var stall = await _context.Stalls.FindAsync(id);
            if (stall == null) return NotFound("Không tìm thấy sạp.");

            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StallId == id);

            if (sub == null)
            {
                sub = new Subscription { StallId = id, ExpiryDate = DateTime.Now.AddDays(30), IsActive = true };
                _context.Subscriptions.Add(sub);
            }
            else
            {
                if (!sub.ExpiryDate.HasValue || sub.ExpiryDate.Value < DateTime.Now)
                {
                    sub.ExpiryDate = DateTime.Now.AddDays(30);
                }
                else
                {
                    sub.ExpiryDate = sub.ExpiryDate.Value.AddDays(30);
                }
                sub.IsActive = true;
            }

            stall.IsOpen = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gia hạn thành công" });
        }

        public class UpdateStallRequest { public int Id { get; set; } public string Name { get; set; } = ""; public bool IsOpen { get; set; } public int? OwnerId { get; set; } public int RadiusMeter { get; set; } public string? TtsScript { get; set; } public IFormFile? ImageFile { get; set; } }
        public class AssignStallRequest { public int StallId { get; set; } public int OwnerId { get; set; } public string NewStallName { get; set; } = ""; }
        public class CreateStallPos { public double Latitude { get; set; } public double Longitude { get; set; } }
    }
}