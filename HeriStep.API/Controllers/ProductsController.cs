using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
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

        [HttpPost("add-with-translate")]
        public async Task<IActionResult> AddProductWithAI([FromForm] AddProductRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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

                var product = new Product
                {
                    StallId = req.StallId,
                    BasePrice = req.Price,
                    IsSignature = req.IsSignature,
                    ImageUrl = savedImageUrl
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _context.ProductTranslations.Add(new ProductTranslation
                {
                    ProductId = product.Id,
                    LangCode = "vi",
                    ProductName = req.NameVi,
                    ProductDesc = "Mn an thom ngon chu?n v? Vinh Khnh"
                });

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
                return Ok(new { message = "Thm mn, luu ?nh v AI d?ch thnh cng!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class AddProductRequest
    {
        public int StallId { get; set; }
        public string NameVi { get; set; } = "";
        public decimal Price { get; set; }
        public bool IsSignature { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
