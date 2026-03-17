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

        // 1. LẤY DANH SÁCH - ĐÃ THÊM ĐẾM SẠP CHUẨN
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tour>>> GetAllTours()
        {
            return await _context.Tours
                .Select(t => new Tour
                {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    IsActive = t.IsActive,
                    IsTopHot = t.IsTopHot,
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id)
                })
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        // 2. CHI TIẾT LỘ TRÌNH (Dùng cho trang Details.cshtml)
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Tour>> GetTourDetails(int id)
        {
            var tour = await _context.Tours
                .Select(t => new Tour
                {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    IsActive = t.IsActive,
                    IsTopHot = t.IsTopHot,
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id),
                    Stalls = _context.Stalls
                                .Where(s => s.TourID == t.Id)
                                .OrderBy(s => s.SortOrder)
                                .ToList()
                })
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null) return NotFound();
            return Ok(tour);
        }

        // 3. LẤY TẤT CẢ SẠP (Để chọn thêm vào lộ trình)
        [HttpGet("available-stalls")]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetAvailableStalls()
        {
            return await _context.Stalls.ToListAsync();
        }

        // 4. THÊM SẠP VÀO LỘ TRÌNH
        [HttpPut("{tourId}/AddStall/{stallId}")]
        public async Task<IActionResult> AddStallToTour(int tourId, int stallId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null) return NotFound();

            var maxOrder = await _context.Stalls
                .Where(s => s.TourID == tourId)
                .MaxAsync(s => (int?)s.SortOrder) ?? 0;

            stall.TourID = tourId;
            stall.SortOrder = maxOrder + 1;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // 5. DI CHUYỂN THỨ TỰ SẠP (Up/Down)
        [HttpPut("{tourId}/MoveStall/{stallId}")]
        public async Task<IActionResult> MoveStall(int tourId, int stallId, [FromQuery] string direction)
        {
            var stalls = await _context.Stalls
                .Where(s => s.TourID == tourId)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();

            var currentIndex = stalls.FindIndex(s => s.Id == stallId);
            if (currentIndex < 0) return NotFound();

            int targetIndex = direction == "up" ? currentIndex - 1 : currentIndex + 1;

            if (targetIndex >= 0 && targetIndex < stalls.Count)
            {
                int tempOrder = stalls[currentIndex].SortOrder;
                stalls[currentIndex].SortOrder = stalls[targetIndex].SortOrder;
                stalls[targetIndex].SortOrder = tempOrder;

                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest("Không thể di chuyển.");
        }

        // 6. GỠ SẠP KHỎI LỘ TRÌNH
        [HttpPut("{tourId}/RemoveStall/{stallId}")]
        public async Task<IActionResult> RemoveStallFromTour(int tourId, int stallId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null || stall.TourID != tourId) return NotFound();

            stall.TourID = null;
            stall.SortOrder = 0;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 7. LẤY TOP HOT (Dùng cho Mobile/Client)
        [HttpGet("top-hot")]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTopHotTours()
        {
            var hotTours = await _context.Tours
                .Where(t => t.IsActive == true && t.IsTopHot == true)
                .ToListAsync();

            if (!hotTours.Any())
            {
                hotTours = await _context.Tours.Where(t => t.IsActive == true).Take(10).ToListAsync();
            }
            return Ok(hotTours);
        }

        // 8. CRUD CƠ BẢN
        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] Tour tour)
        {
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return Ok(tour);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] Tour tour)
        {
            if (id != tour.Id) return BadRequest();
            _context.Entry(tour).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            // Gỡ sạp ra trước khi xóa Tour để tránh lỗi Database
            var stalls = await _context.Stalls.Where(s => s.TourID == id).ToListAsync();
            foreach (var s in stalls) s.TourID = null;

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 9. TẠO 10 TOUR NGẪU NHIÊN (Nút "Tạo dữ liệu ảo")
        [HttpPost("generate-daily")]
        public async Task<IActionResult> GenerateDailyTours()
        {
            var tourCount = await _context.Tours.CountAsync();
            if (tourCount >= 10)
            {
                return BadRequest("Hệ thống đã có đủ lộ trình rồi, sếp đừng xóa tội nghiệp em!");
            }
            var allStalls = await _context.Stalls.ToListAsync();
            foreach (var s in allStalls) { s.TourID = null; s.SortOrder = 0; }

            var oldTours = await _context.Tours.ToListAsync();
            _context.Tours.RemoveRange(oldTours);
            await _context.SaveChangesAsync();

            var random = new Random();
            for (int i = 1; i <= 10; i++)
            {
                var newTour = new Tour
                {
                    TourName = $"Lộ trình Ẩm thực #{i}",
                    Description = "Hệ thống tự động đề xuất.",
                    IsActive = true,
                    IsTopHot = (i <= 3)
                };
                _context.Tours.Add(newTour);
                await _context.SaveChangesAsync();

                var randomStalls = allStalls.Where(s => s.TourID == null).OrderBy(x => Guid.NewGuid()).Take(random.Next(3, 6)).ToList();
                int order = 1;
                foreach (var stall in randomStalls)
                {
                    stall.TourID = newTour.Id;
                    stall.SortOrder = order++;
                }
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "OK" });
        }
    }
}