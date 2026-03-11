using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public ToursController(HeriStepDbContext context) => _context = context;

        // [GET] Dành cho Admin: Kéo tất cả Tour và đếm luôn số sạp hàng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetAllTours()
        {
            // Logic dựa trên DB: Lấy tour_name, image_url, is_active
            return await _context.Tours
                .Select(t => new Tour
                {
                    Id = t.Id,
                    TourName = t.TourName, // Tự động map từ cột tour_name
                    Description = t.Description,
                    ImageUrl = t.ImageUrl, // Tự động map từ cột image_url
                    IsActive = t.IsActive, // Tự động map từ cột is_active
                    IsTopHot = t.IsTopHot, // Tự động map từ cột is_top_hot
                    // Đếm số sạp dựa trên FK TourID trong bảng Stalls
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id)
                })
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        // [GET] Dành cho App Mobile: Trả về Top 10 Tour Hot (Dùng cho trang chủ App)
        [HttpGet("top-hot")]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTopHotTours()
        {
            // 1. Lấy những tour được Bot đánh dấu IsTopHot = true
            var hotTours = await _context.Tours
                .Where(t => t.IsActive == true && t.IsTopHot == true)
                .ToListAsync();

            // 2. FALLBACK: Nếu DB mới tinh chưa có tour hot, lấy 10 tour mới nhất để App không bị trống
            if (!hotTours.Any())
            {
                hotTours = await _context.Tours
                    .Where(t => t.IsActive == true)
                    .OrderByDescending(t => t.Id)
                    .Take(10)
                    .ToListAsync();
            }

            return Ok(hotTours);
        }

        // [POST] Thêm Tour mới (Dành cho Admin)
        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] Tour tour)
        {
            // DB IDENTITY(1,1) nên không cần truyền ID
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return Ok(tour);
        }

        // [PUT] Cập nhật lộ trình
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] Tour tour)
        {
            if (id != tour.Id) return BadRequest();

            // Entity Framework sẽ tự động sinh lệnh UPDATE khớp với tên cột trong DB
            _context.Entry(tour).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tours.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return Ok();
        }

        // [DELETE] Xóa Lộ trình (Ràng buộc ON DELETE SET NULL trong DB của bạn)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            // Kiểm tra xem lộ trình có sạp nào không trước khi cho xóa (để an toàn cho dữ liệu)
            var hasStalls = await _context.Stalls.AnyAsync(s => s.TourID == id);
            if (hasStalls)
            {
                return BadRequest("Lộ trình này đang chứa sạp hàng. Vui lòng gỡ sạp trước khi xóa lộ trình!");
            }

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}