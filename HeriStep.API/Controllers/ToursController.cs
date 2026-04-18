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
        // 1. HIỂN THỊ DANH SÁCH & THỐNG KÊ (DÀNH CHO KHÁCH & ADMIN)
        // ==========================================================

        [HttpGet("top10")]
        public async Task<IActionResult> GetTop10Tours()
        {
            var now = DateTime.Now;

            var topTours = await _context.Tours
                .OrderByDescending(t => t.Id)
                .Take(10)
                .Select(t => new {
                    Id          = t.Id,
                    TourName    = t.TourName,
                    Description = string.IsNullOrEmpty(t.Description) ? "Lộ trình ăn vặt quận 4 độc đáo" : t.Description,
                    Rating      = 4.8,
                    StallsCount = _context.Stalls.Count(s => s.TourID == t.Id
                                                              && s.IsOpen == true
                                                              && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now)),
                    Price    = 50000,
                    ImageUrl = string.IsNullOrEmpty(t.ImageUrl)
                        ? "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600&auto=format&fit=crop"
                        : t.ImageUrl
                })
                .ToListAsync();

            return Ok(topTours);
        }

        // Alias cho client cũ dùng /api/Tours/top-hot
        [HttpGet("top-hot")]
        public Task<IActionResult> GetTopHotTours() => GetTop10Tours();

        [HttpGet]
        public async Task<IActionResult> GetTours([FromQuery] DateTime? updatedAfter = null)
        {
            var now = DateTime.Now;

            var tours = await _context.Tours
                .Where(t => !updatedAfter.HasValue || (t.UpdatedAt.HasValue && t.UpdatedAt > updatedAfter.Value))
                .Select(t => new {
                    Id          = t.Id,
                    TourName    = t.TourName,
                    Description = t.Description,
                    IsActive    = t.IsActive,
                    IsTopHot    = t.IsTopHot,
                    ImageUrl    = t.ImageUrl,
                    UpdatedAt   = t.UpdatedAt,
                    StallCount  = _context.Stalls.Count(s => s.TourID == t.Id
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
                .Include(t => t.Stalls.Where(s => s.IsOpen == true
                    && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now)))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null) return NotFound("Không tìm thấy lộ trình này.");

            if (tour.Stalls != null)
            {
                tour.Stalls   = tour.Stalls.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
                tour.StallCount = tour.Stalls.Count;
            }
            else
            {
                tour.StallCount = 0;
            }

            return Ok(tour);
        }

        // ==========================================================
        // 2. CRUD CHÍNH CHO ADMIN QUẢN LÝ LỘ TRÌNH
        // ==========================================================

        [HttpPost]
        public async Task<IActionResult> CreateTour([FromBody] Tour tour)
        {
            if (string.IsNullOrWhiteSpace(tour.TourName))
                return BadRequest("Tên lộ trình không được để trống");

            tour.IsActive  = true;
            tour.IsDeleted = false;
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

            existing.TourName    = tour.TourName;
            existing.Description = tour.Description;
            existing.ImageUrl    = tour.ImageUrl;
            existing.IsActive    = tour.IsActive;
            existing.IsTopHot    = tour.IsTopHot;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            var tour = await _context.Tours.Include(t => t.Stalls).FirstOrDefaultAsync(t => t.Id == id);
            if (tour == null) return NotFound();

            // Nhả tự do cho các sạp — không xóa sạp của Merchant
            if (tour.Stalls != null && tour.Stalls.Any())
            {
                foreach (var s in tour.Stalls)
                {
                    s.TourID    = null;
                    s.SortOrder = 0;
                }
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa lộ trình thành công!" });
        }

        // ==========================================================
        // 3. AUTO-ASSIGN: Phân bổ sạp vòng tròn vào các Tour hiện có
        // ==========================================================
        [HttpPost("auto-assign")]
        public async Task<IActionResult> FullRedistributeStalls()
        {
            var now = DateTime.Now;

            var tours = await _context.Tours.Where(t => t.IsActive == true).ToListAsync();
            if (!tours.Any()) return BadRequest("Hệ thống chưa có lộ trình nào!");

            var validStalls = await _context.Stalls
                .Where(s => s.IsOpen == true
                    && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now))
                .ToListAsync();

            if (!validStalls.Any())
                return BadRequest("Không có sạp nào đủ điều kiện (Mở cửa & Còn hạn) để phân bổ!");

            var rand = new Random();
            var shuffledStalls = validStalls.OrderBy(x => rand.Next()).ToList();

            for (int i = 0; i < shuffledStalls.Count; i++)
            {
                var targetTour = tours[i % tours.Count];
                shuffledStalls[i].TourID    = targetTour.Id;
                shuffledStalls[i].SortOrder = (i / tours.Count) + 1;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã quy hoạch lại toàn bộ {shuffledStalls.Count} sạp hợp lệ vào {tours.Count} lộ trình."
            });
        }

        // ==========================================================
        // 4. CREATE-TRENDING: Tự tạo Tour từ Top X sạp có lượt nghe cao nhất
        //    Admin gọi: POST /api/tours/create-trending?topX=5
        // ==========================================================
        [HttpPost("create-trending")]
        public async Task<IActionResult> CreateTrendingTour([FromQuery] int topX = 5)
        {
            if (topX < 1 || topX > 50)
                return BadRequest("topX phải nằm trong khoảng 1 - 50.");

            try
            {
                // 1. Lấy Top X sạp theo tổng thời gian nghe audio (ListenDurationSeconds)
                var topStallIds = await _context.StallVisits
                    .AsNoTracking()
                    .Where(v => v.ListenDurationSeconds > 0)
                    .GroupBy(v => v.StallId)
                    .Select(g => new
                    {
                        StallId            = g.Key,
                        TotalListenSeconds = g.Sum(x => x.ListenDurationSeconds)
                    })
                    .OrderByDescending(x => x.TotalListenSeconds)
                    .Take(topX)
                    .Select(x => x.StallId)
                    .ToListAsync();

                if (!topStallIds.Any())
                    return BadRequest("Chưa có dữ liệu nghe audio nào trong hệ thống để xếp Top.");

                // 2. Tạo Tour mới có tên theo ngày hiện tại
                var newTour = new Tour
                {
                    TourName    = $"🔥 Top Trending - {DateTime.Now:dd/MM/yyyy}",
                    Description = $"Tự động tạo ngày {DateTime.Now:dd/MM/yyyy HH:mm} từ Top {topX} sạp được nghe nhiều nhất.",
                    IsActive    = true,
                    IsTopHot    = true,
                    IsDeleted   = false,
                    ImageUrl    = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600"
                };

                _context.Tours.Add(newTour);
                await _context.SaveChangesAsync(); // Lấy được Id mới

                // 3. Gán các sạp vào Tour vừa tạo theo thứ tự
                var stallsToAssign = await _context.Stalls
                    .Where(s => topStallIds.Contains(s.Id) && s.IsDeleted == false)
                    .ToListAsync();

                for (int i = 0; i < stallsToAssign.Count; i++)
                {
                    stallsToAssign[i].TourID    = newTour.Id;
                    stallsToAssign[i].SortOrder = i + 1;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message   = $"Đã tạo Tour Trending '{newTour.TourName}' với {stallsToAssign.Count} sạp hàng.",
                    tourId    = newTour.Id,
                    tourName  = newTour.TourName,
                    stallCount = stallsToAssign.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo Tour Trending.", detail = ex.Message });
            }
        }

        // ==========================================================
        // 5. QUẢN LÝ SẠP TRONG LỘ TRÌNH (TRANG DETAILS)
        // ==========================================================

        [HttpGet("available-stalls")]
        public async Task<IActionResult> GetAvailableStalls()
        {
            var now = DateTime.Now;

            var stalls = await _context.Stalls
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

            var maxSort = await _context.Stalls
                .Where(s => s.TourID == tourId)
                .MaxAsync(s => (int?)s.SortOrder) ?? 0;
            stall.SortOrder = maxSort + 1;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{tourId}/RemoveStall/{stallId}")]
        public async Task<IActionResult> RemoveStallFromTour(int tourId, int stallId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null || stall.TourID != tourId) return NotFound();

            stall.TourID    = null;
            stall.SortOrder = 0;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{tourId}/MoveStall/{stallId}")]
        public async Task<IActionResult> MoveStall(int tourId, int stallId, [FromQuery] string direction)
        {
            var now = DateTime.Now;

            var tour = await _context.Tours
                .Include(t => t.Stalls.Where(s => s.IsOpen == true
                    && _context.Subscriptions.Any(sub => sub.StallId == s.Id && sub.ExpiryDate > now)))
                .FirstOrDefaultAsync(t => t.Id == tourId);

            if (tour == null || tour.Stalls == null) return NotFound("Lỗi dữ liệu.");

            var stalls       = tour.Stalls.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
            var currentIndex = stalls.FindIndex(s => s.Id == stallId);

            if (currentIndex == -1) return NotFound();

            if (direction == "up" && currentIndex > 0)
            {
                (stalls[currentIndex], stalls[currentIndex - 1]) = (stalls[currentIndex - 1], stalls[currentIndex]);
            }
            else if (direction == "down" && currentIndex < stalls.Count - 1)
            {
                (stalls[currentIndex], stalls[currentIndex + 1]) = (stalls[currentIndex + 1], stalls[currentIndex]);
            }

            for (int i = 0; i < stalls.Count; i++)
                stalls[i].SortOrder = i + 1;

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
