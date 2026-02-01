using HeriStep.API.Data;
using HeriStep.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers // Thêm namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public ToursController(HeriStepDbContext context) => _context = context;

        // 1. LẤY DANH SÁCH (Tối ưu hóa: Lấy tất cả trong 1 câu lệnh SQL)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTours()
        {
            return await _context.Tours
                .Select(t => new Tour
                {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    IsActive = t.IsActive,
                    // Đếm số sạp ngay tại đây để tránh vòng lặp foreach
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id)
                })
                .ToListAsync();
        }

        // 2. LẤY CHI TIẾT THEO ID (Cần thiết cho CreatedAtAction)
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            tour.StallCount = await _context.Stalls.CountAsync(s => s.TourID == id);
            return tour;
        }

        [HttpPost]
        public async Task<ActionResult<Tour>> PostTour(Tour tour)
        {
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            // Sửa lại thành GetTour để đúng chuẩn RESTful
            return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, tour);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            if (await _context.Stalls.AnyAsync(s => s.TourID == id))
                return BadRequest("Không thể xóa Tour đang có sạp hàng liên kết!");

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}