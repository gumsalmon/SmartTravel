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

        public ToursController(HeriStepDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. KHU VỰC HIỂN THỊ DANH SÁCH & THỐNG KÊ (DÀNH CHO KHÁCH & ADMIN)
        // ==========================================================

        [HttpGet("top10")]
        public async Task<IActionResult> GetTop10Tours()
        {
            var now = DateTime.Now;

            var topTours = await _context.Tours
                .OrderByDescending(t => t.Id)
                .Take(10)
                .Select(t => new {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = string.IsNullOrEmpty(t.Description) ? "Lộ trình ăn vặt quận 4 độc đáo" : t.Description,
                    Rating = 4.8,
<<<<<<< HEAD
                    StallsCount = _context.Stalls.Count(s => s.TourID == t.Id && s.IsOpen == true),
=======
                    // 💡 TECH LEAD FIX: Chỉ đếm sạp ĐANG MỞ (== true) và CÒN HẠN
                    StallsCount = _context.Stalls.Count(s => s.TourID == t.Id
                                                          && s.IsOpen == true
                                                          && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now)),
>>>>>>> b89556b3633944a7b82b6cc91b72bbf8f688426a
                    Price = 50000,
                    ImageUrl = string.IsNullOrEmpty(t.ImageUrl) ? "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600&auto=format&fit=crop" : t.ImageUrl
                })
                .ToListAsync();

            return Ok(topTours);
        }

        // Alias for legacy client calls using /api/Tours/top-hot
        [HttpGet("top-hot")]
        public Task<IActionResult> GetTopHotTours() => GetTop10Tours();

        [HttpGet]
        public async Task<IActionResult> GetTours()
        {
            var now = DateTime.Now;

            var tours = await _context.Tours
                .Select(t => new {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    IsTopHot = t.IsTopHot,
                    ImageUrl = t.ImageUrl,
                    // 💡 TECH LEAD FIX: Lọc sạp đóng cửa/hết hạn
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id
                                                         && s.IsOpen == true
                                                         && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now))
                })
                .ToListAsync();
            return Ok(tours);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var now = DateTime.Now;

            var tour = await _context.Tours
                // 💡 TECH LEAD FIX: Filtered Include - Dọn sạch sạp đóng cửa/hết hạn ngay từ lúc query DB
                .Include(t => t.Stalls.Where(s => s.IsOpen == true && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now)))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null) return NotFound("Không tìm thấy lộ trình này.");

            // Ép danh sách sạp xếp theo SortOrder trước khi trả về
            if (tour.Stalls != null)
            {
                tour.Stalls = tour.Stalls.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
                tour.StallCount = tour.Stalls.Count;
            }
            else
            {
                tour.StallCount = 0;
            }

            return Ok(tour);
        }

        // ==========================================================
        // 2. KHU VỰC CRUD CHÍNH CHO WEB ADMIN QUẢN LÝ LỘ TRÌNH
        // ==========================================================

        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] Tour tour)
        {
            if (string.IsNullOrWhiteSpace(tour.TourName)) return BadRequest("Tên lộ trình không được để trống");

            tour.IsActive = true;
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return Ok(tour);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] Tour tour)
        {
            if (id != tour.Id) return BadRequest("Lỗi dữ liệu");

            var existing = await _context.Tours.FindAsync(id);
            if (existing == null) return NotFound("Không tìm thấy lộ trình");

            existing.TourName = tour.TourName;
            existing.Description = tour.Description;
            existing.ImageUrl = tour.ImageUrl;
            existing.IsActive = tour.IsActive;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.Include(t => t.Stalls).FirstOrDefaultAsync(t => t.Id == id);
            if (tour == null) return NotFound();

            // Nhả tự do cho các sạp thuộc lộ trình này (Tránh xóa nhầm sạp của khách)
            if (tour.Stalls != null && tour.Stalls.Any())
            {
                foreach (var s in tour.Stalls)
                {
                    s.TourID = null;
                    s.SortOrder = 0;
                }
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa lộ trình thành công!" });
        }

        [HttpPost("auto-assign")]
        public async Task<IActionResult> FullRedistributeStalls()
        {
            var now = DateTime.Now;

            var tours = await _context.Tours.Where(t => t.IsActive == true).ToListAsync();
            if (!tours.Any()) return BadRequest("Hệ thống chưa có lộ trình nào!");

            // 💡 TECH LEAD FIX: Lọc sạp hợp lệ (Đang mở & Còn hạn)
            var validStalls = await _context.Stalls
                .Where(s => s.IsOpen == true && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now))
                .ToListAsync();

            if (!validStalls.Any()) return BadRequest("Không có sạp nào đủ điều kiện (Mở cửa & Còn hạn) để phân bổ!");

            // Xáo bài (Shuffle) để đảm bảo tính ngẫu nhiên
            var rand = new Random();
            var shuffledStalls = validStalls.OrderBy(x => rand.Next()).ToList();

            // Phân bổ vòng tròn (Round Robin) để chia đều sạp cho các Tour
            for (int i = 0; i < shuffledStalls.Count; i++)
            {
                var targetTour = tours[i % tours.Count];
                shuffledStalls[i].TourID = targetTour.Id;
                shuffledStalls[i].SortOrder = (i / tours.Count) + 1;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã quy hoạch lại toàn bộ {shuffledStalls.Count} sạp hợp lệ vào {tours.Count} lộ trình.",
            });
        }

        // ==========================================================
        // 3. KHU VỰC QUẢN LÝ SẠP BÊN TRONG LỘ TRÌNH (TRANG DETAILS)
        // ==========================================================

        [HttpGet("available-stalls")]
        public async Task<IActionResult> GetAvailableStalls()
        {
            var now = DateTime.Now;

            var stalls = await _context.Stalls
                // Bộ lọc 3 tầng cho Dropdown thêm sạp
                .Where(s => s.TourID == null
                         && s.IsOpen == true
                         && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now))
                .ToListAsync();

            return Ok(stalls);
        }

        [HttpPut("{tourId}/AddStall/{stallId}")]
        public async Task<IActionResult> AddStallToTour(int tourId, int stallId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null) return NotFound();

            stall.TourID = tourId;

            // Nhét vào cuối danh sách hiện tại
            var maxSort = await _context.Stalls.Where(s => s.TourID == tourId).MaxAsync(s => (int?)s.SortOrder) ?? 0;
            stall.SortOrder = maxSort + 1;

            await _context.SaveChangesAsync();
            return Ok();
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

        [HttpPut("{tourId}/MoveStall/{stallId}")]
        public async Task<IActionResult> MoveStall(int tourId, int stallId, [FromQuery] string direction)
        {
            var now = DateTime.Now;

            // Filtered Include: Chỉ lấy những sạp hợp lệ đang hiển thị trên giao diện để đổi chỗ
            var tour = await _context.Tours
                .Include(t => t.Stalls.Where(s => s.IsOpen == true && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now)))
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null || tour.Stalls == null) return NotFound("Lỗi dữ liệu.");

            var stalls = tour.Stalls.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
            var currentIndex = stalls.FindIndex(s => s.Id == stallId);

            if (currentIndex == -1) return NotFound();

            // Swap
            if (direction == "up" && currentIndex > 0)
            {
                var temp = stalls[currentIndex];
                stalls[currentIndex] = stalls[currentIndex - 1];
                stalls[currentIndex - 1] = temp;
            }
            else if (direction == "down" && currentIndex < stalls.Count - 1)
            {
                var temp = stalls[currentIndex];
                stalls[currentIndex] = stalls[currentIndex + 1];
                stalls[currentIndex + 1] = temp;
            }

            // Ghi đè lại SortOrder chuẩn
            for (int i = 0; i < stalls.Count; i++)
            {
                stalls[i].SortOrder = i + 1;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
