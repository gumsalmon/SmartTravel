using HeriStep.API.Data;
using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        // =======================================================
        // 1. QUẢN LÝ SẠP (CRUD & LỌC DANH SÁCH)
        // =======================================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetStalls()
        {
            var query = from s in _context.Stalls
                        where !s.IsDeleted
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
            var stall = await _context.Stalls.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng này hoặc sạp đã bị xóa!" });

            var ttsContent = await _context.StallContents
                .Where(c => c.StallId == id && c.LangCode == "vi" && !c.IsDeleted)
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

                var stall = await _context.Stalls.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
                if (stall == null) return NotFound(new { message = "Không tìm thấy sạp hàng này!" });

                // 1. XỬ LÝ UPLOAD ẢNH
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

                // 2. CẬP NHẬT THÔNG TIN SẠP
                stall.Name = req.Name;
                stall.IsOpen = req.IsOpen;
                stall.OwnerId = req.OwnerId;
                stall.RadiusMeter = req.RadiusMeter;
                stall.UpdatedAt = DateTime.Now;

                // 3. XỬ LÝ ĐA NGÔN NGỮ (UPSERT)
                if (!string.IsNullOrWhiteSpace(req.TtsScript))
                {
                    var currentTime = DateTime.Now;
                    var existingContents = await _context.StallContents.Where(c => c.StallId == id).ToListAsync();

                    var viContent = existingContents.FirstOrDefault(c => c.LangCode == "vi");
                    if (viContent != null)
                    {
                        viContent.TtsScript = req.TtsScript;
                        viContent.IsProcessed = true;
                        viContent.IsDeleted = false;
                        viContent.IsActive = true;
                        viContent.UpdatedAt = currentTime;
                    }
                    else
                    {
                        _context.StallContents.Add(new StallContent
                        {
                            StallId = id,
                            LangCode = "vi",
                            TtsScript = req.TtsScript,
                            IsActive = true,
                            IsProcessed = true,
                            UpdatedAt = currentTime
                        });
                    }

                    string[] foreignLangs = { "en", "ja", "ko", "zh", "fr", "es", "ru", "th", "de" };
                    foreach (var lang in foreignLangs)
                    {
                        var fContent = existingContents.FirstOrDefault(c => c.LangCode == lang);
                        if (fContent != null)
                        {
                            fContent.TtsScript = "";
                            fContent.IsProcessed = false;
                            fContent.IsDeleted = false;
                            fContent.IsActive = true;
                            fContent.UpdatedAt = currentTime;
                        }
                        else
                        {
                            _context.StallContents.Add(new StallContent
                            {
                                StallId = id,
                                LangCode = lang,
                                TtsScript = "",
                                IsActive = true,
                                IsProcessed = false,
                                UpdatedAt = currentTime
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thông tin sạp thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật sạp", detail = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStall(int id)
        {
            try
            {
                var stall = await _context.Stalls.FindAsync(id);
                if (stall == null || stall.IsDeleted)
                    return NotFound(new { message = "Không tìm thấy sạp hoặc sạp đã bị xóa từ trước!" });

                stall.IsDeleted = true;
                stall.IsOpen = false;
                stall.UpdatedAt = DateTime.Now;
                stall.TourID = null;
                stall.SortOrder = 0;

                var contents = await _context.StallContents.Where(c => c.StallId == id && !c.IsDeleted).ToListAsync();
                foreach (var c in contents)
                {
                    c.IsDeleted = true;
                    c.UpdatedAt = DateTime.Now;
                }

                var products = await _context.Products.Where(p => p.StallId == id && !p.IsDeleted).ToListAsync();
                foreach (var p in products)
                {
                    p.IsDeleted = true;
                    p.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã xóa sạp hàng an toàn!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xóa sạp", detail = ex.Message });
            }
        }

        // =======================================================
        // 2. API CHO MAP, MOBILE APP VÀ CHỦ SẠP
        // =======================================================

        [HttpGet("admin-map")]
        public async Task<IActionResult> GetAllStallsForMap()
        {
            var stalls = await _context.Stalls
                .Where(s => !s.IsDeleted)
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

        // 💡 [QUAN TRỌNG] API CHUYÊN DỤNG CHO TRANG CHỦ SẠP (ĐÃ CÓ isExpired)
        [HttpGet("my-stalls")]
        public async Task<IActionResult> GetMyStalls([FromQuery] int? ownerId)
        {
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int finalOwnerId = ownerId ?? (int.TryParse(claimId, out var id) ? id : 0);

            if (finalOwnerId == 0) return Unauthorized(new { message = "Không xác định được chủ sạp. Vui lòng đăng nhập lại!" });

            var stalls = await _context.Stalls
                .Where(s => s.OwnerId == finalOwnerId && !s.IsDeleted)
                .Select(s => new
                {
                    Id = s.Id,
                    Name = s.Name ?? "Sạp chưa đặt tên",
                    OwnerId = s.OwnerId,
                    ImageUrl = s.ImageUrl,
                    IsOpen = s.IsOpen,
                    IsExpired = !_context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > DateTime.Now)
                })
                .OrderBy(s => s.Id)
                .ToListAsync();

            return Ok(stalls);
        }

        [HttpGet("top5")]
        public async Task<IActionResult> GetTop5Stalls()
        {
            var top = await _context.Stalls
                .Where(s => s.IsOpen && !s.IsDeleted)
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

        [HttpGet("{id}/tts/{langCode}")]
        public async Task<IActionResult> GetStallTts(int id, string langCode)
        {
            var content = await _context.StallContents
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.StallId == id && c.LangCode == langCode && !c.IsDeleted);

            if (content == null || string.IsNullOrWhiteSpace(content.TtsScript))
                return NotFound(new { message = "Không tìm thấy TTS hoặc đang chờ dịch" });

            return Ok(new { text = content.TtsScript });
        }

        // =======================================================
        // 3. QUẢN LÝ MÓN ĂN & GÓI CƯỚC (PRODUCTS / SUBSCRIPTIONS)
        // =======================================================

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
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = DateTime.Now.Ticks.ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                    imageUrl = "/images/products/" + uniqueFileName;
                }

                var newProduct = new Product { StallId = StallId, BasePrice = BasePrice, ImageUrl = imageUrl, IsSignature = false, UpdatedAt = DateTime.Now };
                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();

                var currentTime = DateTime.Now;
                _context.ProductTranslations.Add(new ProductTranslation { ProductId = newProduct.Id, LangCode = "vi", ProductName = Name, IsProcessed = true, UpdatedAt = currentTime });

                string[] foreignLangs = { "en", "ja", "ko", "zh", "fr", "es", "ru", "th", "de" };
                foreach (var lang in foreignLangs)
                {
                    _context.ProductTranslations.Add(new ProductTranslation
                    {
                        ProductId = newProduct.Id,
                        LangCode = lang,
                        ProductName = "",
                        IsProcessed = false,
                        UpdatedAt = currentTime
                    });
                }
                await _context.SaveChangesAsync();

                return Ok(new { message = "Thêm món ăn thành công!", imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("update-product/{productId}")]
        public async Task<IActionResult> UpdateProduct(int productId, [FromForm] string Name, [FromForm] decimal BasePrice, IFormFile? ImageFile)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return NotFound(new { message = "Không tìm thấy món ăn này!" });

                product.BasePrice = BasePrice;
                product.UpdatedAt = DateTime.Now;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (ImageFile.Length > 5 * 1024 * 1024)
                        return BadRequest(new { message = "File ảnh không được vượt quá 5MB!" });

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles", "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = DateTime.Now.Ticks.ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }

                var viTranslation = await _context.ProductTranslations.FirstOrDefaultAsync(t => t.ProductId == productId && t.LangCode == "vi");
                if (viTranslation != null)
                {
                    viTranslation.ProductName = Name;
                    viTranslation.UpdatedAt = DateTime.Now;

                    var foreignLangs = await _context.ProductTranslations.Where(t => t.ProductId == productId && t.LangCode != "vi").ToListAsync();
                    foreach (var lang in foreignLangs)
                    {
                        lang.IsProcessed = false;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật món ăn thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật món ăn", detail = ex.Message });
            }
        }

        [HttpDelete("delete-product/{productId}")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.IsDeleted)
                    return NotFound("Không tìm thấy món ăn này hoặc đã bị xóa!");

                product.IsDeleted = true;
                product.UpdatedAt = DateTime.Now;

                var translations = await _context.ProductTranslations.Where(t => t.ProductId == productId && !t.IsDeleted).ToListAsync();
                foreach (var t in translations)
                {
                    t.IsDeleted = true;
                    t.UpdatedAt = DateTime.Now;
                }

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
                var products = await _context.Products
                    .Where(p => p.StallId == stallId && !p.IsDeleted)
                    .Select(p => new
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

        [HttpGet("available-owners")]
        public async Task<IActionResult> GetAvailableOwners()
        {
            var owners = await _context.Users
                .Where(u => !u.IsDeleted)
                .Select(u => new { id = u.Id, fullName = u.FullName ?? "Chưa cập nhật tên", username = u.Username })
                .ToListAsync();
            return Ok(owners);
        }

        [HttpPost("extend-subscription/{id}")]
        public async Task<IActionResult> ExtendSubscription(int id)
        {
            var stall = await _context.Stalls.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            if (stall == null) return NotFound("Không tìm thấy sạp.");

            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StallId == id);

            if (sub == null)
            {
                sub = new Subscription { StallId = id, ExpiryDate = DateTime.Now.AddDays(30), IsActive = true, UpdatedAt = DateTime.Now };
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
                sub.UpdatedAt = DateTime.Now;
            }

            stall.IsOpen = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gia hạn thành công" });
        }

        // =======================================================
        // DTOs (Data Transfer Objects)
        // =======================================================
        public class UpdateStallRequest { public int Id { get; set; } public string Name { get; set; } = ""; public bool IsOpen { get; set; } public int? OwnerId { get; set; } public int RadiusMeter { get; set; } public string? TtsScript { get; set; } public IFormFile? ImageFile { get; set; } }
        public class AssignStallRequest { public int StallId { get; set; } public int OwnerId { get; set; } public string NewStallName { get; set; } = ""; }
        public class CreateStallPos { public double Latitude { get; set; } public double Longitude { get; set; } }

        // DTO nội bộ cho GetStalls (Để tránh gọi đè vào Shared.Models nếu bị xung đột)
        public class PointOfInterest { public int Id { get; set; } public string? Name { get; set; } public double Latitude { get; set; } public double Longitude { get; set; } public int RadiusMeter { get; set; } public bool IsOpen { get; set; } public string? ImageUrl { get; set; } public int? OwnerId { get; set; } public string? OwnerName { get; set; } public DateTime? UpdatedAt { get; set; } }
    }
}