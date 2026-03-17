using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // 💡 Bắt buộc để dùng IFormFile
using System;
using System.IO; // 💡 Bắt buộc để lưu file
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public ProductsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // TÍNH NĂNG AI: THÊM MÓN & TỰ DỊCH & UPLOAD ẢNH
        [HttpPost("add-with-translate")]
        public async Task<IActionResult> AddProductWithAI([FromForm] AddProductRequest req) // 💡 SỬA THÀNH [FromForm]
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. XỬ LÝ UPLOAD ẢNH
                string savedImageUrl = "";
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
                    savedImageUrl = "/uploads/" + fileName;
                }

                // 2. Tạo món ăn mới
                var product = new Product
                {
                    StallId = req.StallId,
                    BasePrice = req.Price,
                    IsSignature = req.IsSignature,
                    ImageUrl = savedImageUrl // 💡 Lưu đường dẫn ảnh vừa upload
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 3. Lưu bản gốc Tiếng Việt
                _context.ProductTranslations.Add(new ProductTranslation
                {
                    ProductId = product.Id,
                    LangCode = "vi",
                    ProductName = req.NameVi,
                    ProductDesc = "Món ăn thơm ngon chuẩn vị Vĩnh Khánh"
                });

                // 4. MÔ PHỎNG AI DỊCH RA 9 NGÔN NGỮ KHÁC
                string[] foreignLangs = { "en", "ja", "ko", "zh", "fr", "es", "ru", "th", "de" };
                foreach (var lang in foreignLangs)
                {
                    _context.ProductTranslations.Add(new ProductTranslation
                    {
                        ProductId = product.Id,
                        LangCode = lang,
                        ProductName = $"{req.NameVi} ({lang.ToUpper()})",
                        ProductDesc = $"[AI Translated to {lang.ToUpper()}] Delicious local dish."
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Thêm món, lưu ảnh và AI dịch thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // 💡 ĐÃ THÊM IFormFile ĐỂ NHẬN ẢNH TỪ WEB
    public class AddProductRequest
    {
        public int StallId { get; set; }
        public string NameVi { get; set; } = "";
        public decimal Price { get; set; }
        public bool IsSignature { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}