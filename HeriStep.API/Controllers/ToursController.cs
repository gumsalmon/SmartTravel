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

        // ==========================================
        // 1. LẤY DANH SÁCH LỘ TRÌNH
        // ==========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTours()
        {
            // 💡 Dùng toán tử ?? ngay trong Select để ép SQL xử lý NULL
            return await _context.Tours
                .Select(t => new Tour
                {
                    Id = t.Id,
                    TourName = t.TourName ?? "Lộ trình không tên",
                    // Nếu Description hoặc ImageUrl trong DB là NULL, gán giá trị mặc định ngay
                    Description = t.Description ?? "",
                    ImageUrl = t.ImageUrl ?? "default-tour.jpg",
                    IsActive = t.IsActive ?? true,

                    // Đếm số sạp liên kết trực tiếp trong 1 câu Query
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id)
                })
                .ToListAsync();
        }

        // ==========================================
        // 2. LẤY CHI TIẾT THEO ID
        // ==========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null) return NotFound();

            // Xử lý an toàn dữ liệu sau khi lấy từ DB
            tour.Description ??= "";
            tour.ImageUrl ??= "default-tour.jpg";
            tour.IsActive ??= true;

            tour.StallCount = await _context.Stalls.CountAsync(s => s.TourID == id);

            return tour;
        }

        // ==========================================
        // 3. THÊM LỘ TRÌNH MỚI
        // ==========================================
        [HttpPost]
        public async Task<ActionResult<Tour>> PostTour(Tour tour)
        {
            // Đảm bảo dữ liệu gửi lên không bị NULL các trường quan trọng
            tour.IsActive ??= true;
            tour.Description ??= "";
            tour.ImageUrl ??= "default-tour.jpg";

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, tour);
        }

        // ==========================================
        // 4. CẬP NHẬT LỘ TRÌNH
        // ==========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTour(int id, Tour tour)
        {
            if (id != tour.Id) return BadRequest();

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

            return NoContent();
        }

        // ==========================================
        // 5. XÓA LỘ TRÌNH (Kèm ràng buộc nghiệp vụ)
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            // Ràng buộc: Không cho xóa nếu lộ trình này đang có sạp hàng hoạt động
            if (await _context.Stalls.AnyAsync(s => s.TourID == id))
            {
                return BadRequest("❌ Không thể xóa! Đang có sạp hàng thuộc lộ trình này.");
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}