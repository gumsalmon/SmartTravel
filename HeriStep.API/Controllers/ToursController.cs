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
                    // Đếm sạp thật thay vì để số cứng
                    StallsCount = _context.Stalls.Count(s => s.TourID == t.Id),
                    Price = 50000,
                    ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=600&auto=format&fit=crop"
                })
                .ToListAsync();

            return Ok(topTours);
        }

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
                    // 💡 BẮT BUỘC: Đếm số lượng sạp đang nằm trong Tour này
                    StallCount = _context.Stalls.Count(s => s.TourID == t.Id)
                })
                .ToListAsync();
            return Ok(tours);
        }

        // 💡 API MỚI: DÀNH CHO NÚT "TRỘN LẠI LỘ TRÌNH" 
        [HttpPost("reshuffle")]
        public async Task<IActionResult> ReshuffleTours()
        {
            var tours = await _context.Tours.ToListAsync();
            if (!tours.Any()) return BadRequest("Hệ thống chưa có lộ trình nào!");

            var openStalls = await _context.Stalls.Where(s => s.IsOpen).ToListAsync();
            if (!openStalls.Any()) return BadRequest("Không có sạp nào đang mở cửa!");

            var rand = new Random();

            // 1. Trả lại trạng thái null cho tất cả sạp
            var allStalls = await _context.Stalls.ToListAsync();
            foreach (var s in allStalls) s.TourID = null;
            await _context.SaveChangesAsync();

            // 2. Lắc xí ngầu, chia lại sạp đang mở vào các Tour
            foreach (var s in openStalls)
            {
                s.TourID = tours[rand.Next(tours.Count)].Id;
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã trộn lại danh sách sạp thành công!" });
        }
    }
}