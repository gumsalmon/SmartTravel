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

        [HttpGet("top10")]
        public async Task<IActionResult> GetTop10Tours()
        {
            var topTours = await _context.Tours
                .OrderByDescending(t => t.Id)
                .Take(10)
                .Select(t => new {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = string.IsNullOrEmpty(t.Description) ? "Lộ trình ăn vặt quận 4 độc đáo" : t.Description,
                    Rating = 4.8,
                    StallsCount = _context.Stalls.Count(s => s.TourID == t.Id && s.IsOpen == true),
                    Price = 50000,
                    ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600&auto=format&fit=crop"
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
            var tours = await _context.Tours
                .Select(t => new {
                    Id = t.Id,
                    TourName = t.TourName,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    IsTopHot = t.IsTopHot,
                    ImageUrl = t.ImageUrl,
                    // 💡 ĐÃ FIX LOGIC: Chỉ đếm những sạp đang mở cửa
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id && s.IsOpen == true)
                })
                .ToListAsync();
            return Ok(tours);
        }

        // 💡 HÀM ĐÃ ĐƯỢC CẬP NHẬT: Lấy chi tiết 1 lộ trình kèm danh sách sạp ĐÃ SẮP XẾP THEO SortOrder
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Stalls)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
            {
                return NotFound("Không tìm thấy lộ trình này.");
            }

            // 💡 QUAN TRỌNG: Ép danh sách sạp phải xếp theo SortOrder trước khi trả về cho Web
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

        // 💡 API MỚI: PHÂN BỔ SẠP BƠ VƠ VÀO TOUR CHỨ KHÔNG ĐẬP ĐI XÂY LẠI
        [HttpPost("auto-assign")]
        public async Task<IActionResult> AutoAssignOrphanStalls()
        {
            var tours = await _context.Tours.ToListAsync();
            if (!tours.Any()) return BadRequest("Hệ thống chưa có lộ trình nào!");

            // 💡 CHỈ LẤY CÁC SẠP ĐANG MỞ CỬA VÀ CHƯA CÓ TOUR (TourID == null)
            var orphanStalls = await _context.Stalls.Where(s => s.IsOpen && s.TourID == null).ToListAsync();

            if (!orphanStalls.Any())
                return BadRequest("Mọi thứ đã ổn định. Không có sạp mới nào cần phân bổ!");

            var rand = new Random();

            // Nhét ngẫu nhiên sạp mồ côi vào các Tour hiện tại
            foreach (var s in orphanStalls)
            {
                s.TourID = tours[rand.Next(tours.Count)].Id;
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã tự động phân bổ {orphanStalls.Count} sạp mới vào các lộ trình!" });
        }

        // ==========================================================
        // KHU VỰC QUẢN LÝ SẠP TRONG LỘ TRÌNH (DETAILS PAGE)
        // ==========================================================

        // Lấy danh sách sạp bơ vơ (để đổ vào Dropdown thêm sạp)
        [HttpGet("available-stalls")]
        public async Task<IActionResult> GetAvailableStalls()
        {
            var stalls = await _context.Stalls
                .Where(s => s.TourID == null && s.IsOpen)
                .ToListAsync();
            return Ok(stalls);
        }

        // Thêm sạp vào lộ trình
        [HttpPut("{tourId}/AddStall/{stallId}")]
        public async Task<IActionResult> AddStallToTour(int tourId, int stallId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null) return NotFound();

            stall.TourID = tourId;

            // Xếp nó nằm cuối cùng
            var maxSort = await _context.Stalls.Where(s => s.TourID == tourId).MaxAsync(s => (int?)s.SortOrder) ?? 0;
            stall.SortOrder = maxSort + 1;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // Gỡ sạp khỏi lộ trình
        [HttpPut("{tourId}/RemoveStall/{stallId}")]
        public async Task<IActionResult> RemoveStallFromTour(int tourId, int stallId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null || stall.TourID != tourId) return NotFound();

            stall.TourID = null;
            stall.SortOrder = 0; // Trả về 0
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ĐỔI THỨ TỰ SẠP
        [HttpPut("{tourId}/MoveStall/{stallId}")]
        public async Task<IActionResult> MoveStall(int tourId, int stallId, [FromQuery] string direction)
        {
            var tour = await _context.Tours
                .Include(t => t.Stalls)
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null || tour.Stalls == null) return NotFound("Lỗi dữ liệu.");

            // Lấy danh sách sạp đang có, xếp chuẩn từ trên xuống dưới
            var stalls = tour.Stalls.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
            var currentIndex = stalls.FindIndex(s => s.Id == stallId);

            if (currentIndex == -1) return NotFound();

            // Logic đổi chỗ trong danh sách ảo
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

            // Ghi đè lại SortOrder từ 1 đến N cho toàn bộ danh sách để đảm bảo không bị trùng số
            for (int i = 0; i < stalls.Count; i++)
            {
                stalls[i].SortOrder = i + 1;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}