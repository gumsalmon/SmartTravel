using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public ToursController(HeriStepDbContext context) => _context = context;

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

        [HttpGet("available-stalls")]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetAvailableStalls()
        {
            var stalls = await _context.Stalls.ToListAsync();
            return Ok(stalls);
        }

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

        [HttpPut("{tourId}/MoveStall/{stallId}")]
        public async Task<IActionResult> MoveStall(int tourId, int stallId, [FromQuery] string direction)
        {
            var stalls = await _context.Stalls
                .Where(s => s.TourID == tourId)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Id)
                .ToListAsync();

            for (int i = 0; i < stalls.Count; i++)
            {
                stalls[i].SortOrder = i + 1;
            }

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
            return BadRequest("Không thể di chuyển theo hướng này.");
        }

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

        [HttpGet("top-hot")]
        public async Task<ActionResult<IEnumerable<Tour>>> GetTopHotTours()
        {
            var hotTours = await _context.Tours
                .Where(t => t.IsActive == true && t.IsTopHot == true)
                .ToListAsync();

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

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tours.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var hasStalls = await _context.Stalls.AnyAsync(s => s.TourID == id);
            if (hasStalls) return BadRequest("Lộ trình này đang chứa sạp hàng. Vui lòng gỡ sạp trước khi xóa!");

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ==========================================
        // TÍNH NĂNG MỚI: TẠO 10 TOUR NGẪU NHIÊN
        // ==========================================
        [HttpPost("generate-daily")]
        public async Task<IActionResult> GenerateDailyTours()
        {
            // 1. Xóa toàn bộ Lộ trình cũ
            var oldTours = await _context.Tours.ToListAsync();
            if (oldTours.Any())
            {
                _context.Tours.RemoveRange(oldTours);
                await _context.SaveChangesAsync();
            }

            // 2. Lấy danh sách tất cả các sạp ĐANG HOẠT ĐỘNG
            var activeStalls = await _context.Stalls.Where(s => s.IsOpen).ToListAsync();
            var random = new Random();

            // 3. Tạo 10 Lộ trình mới
            for (int i = 1; i <= 10; i++)
            {
                var newTour = new Tour
                {
                    TourName = $"Lộ trình Khám phá ẩm thực #{i}",
                    Description = "Lộ trình ngẫu nhiên được hệ thống đề xuất riêng cho ngày hôm nay.",
                    IsActive = true,
                    IsTopHot = (i <= 2) // Cho 2 tour đầu tiên làm Tour HOT
                };

                _context.Tours.Add(newTour);
                await _context.SaveChangesAsync(); // Lưu để lấy ID của Tour mới

                // 4. Bốc ngẫu nhiên 3 đến 5 sạp hàng gán vào Tour này
                int numberOfStalls = random.Next(3, 6);

                // Trộn ngẫu nhiên danh sách sạp
                var randomStalls = activeStalls.OrderBy(x => random.Next()).Take(numberOfStalls).ToList();

                int sortOrder = 1;
                foreach (var stall in randomStalls)
                {
                    stall.TourID = newTour.Id;
                    stall.SortOrder = sortOrder++; // Cập nhật thứ tự luôn cho đẹp
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Đã tạo 10 Lộ trình ngẫu nhiên thành công!" });
        }
    }
}