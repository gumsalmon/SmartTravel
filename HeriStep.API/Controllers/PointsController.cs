using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.Shared;
using System.Linq; // Cần thiết để sử dụng lệnh Join

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PointsController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public PointsController(HeriStepDbContext context)
        {
            _context = context;
        }

        // 1. LẤY DANH SÁCH (JOIN HAI BẢNG)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterest>>> GetPoints()
        {
            // Kết hợp bảng Stalls và StallContents dựa trên stall_id
            var points = await (from s in _context.Stalls
                                join c in _context.StallContents on s.Id equals c.StallId
                                where c.LangCode == "vi" // Chỉ lấy bản thuyết minh tiếng Việt
                                select new PointOfInterest
                                {
                                    Id = s.Id,
                                    Name = s.Name,
                                    Latitude = s.Latitude,
                                    Longitude = s.Longitude,
                                    Radius = s.Radius ?? 0,
                                    ImageUrl = s.ImageUrl,
                                    IsOpen = s.IsOpen,
                                    UpdatedAt = s.UpdatedAt,
                                    TourID = s.TourID ?? 0,
                                    TtsScript = c.TtsScript // Lấy dữ liệu từ bảng nội dung
                                }).ToListAsync();

            return points;
        }

        // 2. THÊM MỚI (LƯU VÀO HAI BẢNG)
        [HttpPost]
        public async Task<ActionResult<PointOfInterest>> PostPoint(PointOfInterest point)
        {
            // BƯỚC A: Lưu thông tin kỹ thuật vào bảng Stalls
            // Tuyệt chiêu dùng JSON để tự động copy dữ liệu từ Point sang Stall
            var jsonCode = System.Text.Json.JsonSerializer.Serialize(point);
            var newStall = System.Text.Json.JsonSerializer.Deserialize<HeriStep.Shared.Stall>(jsonCode);

            _context.Stalls.Add(newStall!);
            await _context.SaveChangesAsync(); // Lưu xong, newStall.Id sẽ tự sinh ra từ SQL

            // Cực kỳ quan trọng: Gán ID mới tạo ngược lại cho point để Bước B xài
            point.Id = newStall!.Id;
            // BƯỚC B: Lưu nội dung thuyết minh vào bảng StallContents
            var content = new StallContent
            {
                StallId = point.Id, // Sử dụng ID vừa tạo ở Bước A
                LangCode = "vi",
                TtsScript = point.TtsScript ?? "",
                IsActive = true
            };
            _context.StallContents.Add(content);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPoints), new { id = point.Id }, point);
        }
    }
}