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
    public class MockDataController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public MockDataController(HeriStepDbContext context)
        {
            _context = context;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateMockData([FromBody] MockDataRequest req)
        {
            var rand = new Random();
            var now = DateTime.Now;

            try
            {
                // 1. Tạo Tours
                var tours = await _context.Tours.ToListAsync();
                if (!tours.Any())
                {
                    for (int i = 1; i <= 10; i++) // Sinh 10 Tour cho phong phú
                    {
                        var t = new Tour { TourName = $"Hành trình di sản {i}", Description = "Mô tả tour khám phá Vĩnh Khánh", IsActive = true };
                        _context.Tours.Add(t);
                    }
                    await _context.SaveChangesAsync();
                    tours = await _context.Tours.ToListAsync();
                }

                // 2. Tạo Users (Chủ sạp)
                var createdUsers = new List<User>();
                for (int i = 0; i < req.UserCount; i++)
                {
                    var user = new User
                    {
                        Username = $"user_{Guid.NewGuid().ToString("N").Substring(0, 5)}",
                        PasswordHash = "123456",
                        FullName = $"Chủ Sạp {i + 1}",
                        Role = "StallOwner"
                    };
                    _context.Users.Add(user);
                    createdUsers.Add(user);
                }
                await _context.SaveChangesAsync();

                // 3. Tạo Sạp & Phân bổ luôn vào Tour
                var createdStalls = new List<Stall>();
                for (int i = 0; i < req.StallCount; i++)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));
                    var stall = new Stall
                    {
                        Name = $"Sạp hàng {rand.Next(100, 999)}",
                        Latitude = 10.76 + (rand.NextDouble() * 0.01),
                        Longitude = 106.70 + (rand.NextDouble() * 0.01),
                        IsOpen = true,
                        RadiusMeter = 50,
                        OwnerId = createdUsers[rand.Next(createdUsers.Count)].Id,
                        TourID = tours[rand.Next(tours.Count)].Id // 💡 ĐÃ FIX: Nhét trực tiếp TourID vào Sạp
                    };
                    _context.Stalls.Add(stall);
                    createdStalls.Add(stall);

                    var sub = new Subscription
                    {
                        StallId = stall.Id,
                        DeviceId = $"HS-DEV-{rand.Next(1000, 9999)}",
                        ActivationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        StartDate = randomDate,
                        ExpiryDate = randomDate.AddDays(30),
                        IsActive = true
                    };
                    _context.Subscriptions.Add(sub);
                }
                await _context.SaveChangesAsync();

                // 💡 CƠ CHẾ SỬA LỖI (SELF-HEALING): 
                // Vét toàn bộ các sạp cũ đang mồ côi (TourID = null) nhét ngẫu nhiên vào Tour
                var orphanStalls = await _context.Stalls.Where(s => s.IsOpen && (s.TourID == null || s.TourID == 0)).ToListAsync();
                if (orphanStalls.Any() && tours.Any())
                {
                    foreach (var s in orphanStalls)
                    {
                        s.TourID = tours[rand.Next(tours.Count)].Id;
                    }
                    await _context.SaveChangesAsync();
                }

                // 4. Tạo Package & Lịch sử mua vé
                var package = await _context.TicketPackages.FirstOrDefaultAsync();
                if (package == null)
                {
                    package = new TicketPackage { PackageName = "Vé Tuần Vĩnh Khánh", Price = 50000, DurationHours = 168, IsActive = true };
                    _context.TicketPackages.Add(package);
                    await _context.SaveChangesAsync();
                }

                for (int i = 0; i < req.VisitCount; i++)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));
                    _context.TouristTickets.Add(new TouristTicket
                    {
                        TicketCode = $"TC-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                        DeviceId = $"DEV-{rand.Next(1000, 9999)}",
                        PackageId = package.Id,
                        AmountPaid = package.Price,
                        CreatedAt = randomDate,
                        ExpiryDate = randomDate.AddHours(package.DurationHours)
                    });

                    if (createdStalls.Any())
                    {
                        _context.StallVisits.Add(new StallVisit
                        {
                            StallId = createdStalls[rand.Next(createdStalls.Count)].Id,
                            DeviceId = $"DEV-{rand.Next(1000, 9999)}",
                            VisitedAt = randomDate
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Mock data đã khớp 100% với cấu trúc DB VinhKhanhTour!" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { error = "Lỗi khi lưu DB", detail = msg });
            }
        }

        [HttpPost("simulate-tourist/{stallId}/{langCode}")]
        public async Task<IActionResult> SimulateTourist(int stallId, string langCode, [FromQuery] string deviceId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp" });

            string fakeDeviceId = !string.IsNullOrEmpty(deviceId) ? deviceId : $"MOCK-{langCode.ToUpper()}-{Guid.NewGuid().ToString().Substring(0, 6)}";

            var visit = new StallVisit
            {
                StallId = stallId,
                DeviceId = fakeDeviceId,
                VisitedAt = DateTime.Now
            };

            _context.StallVisits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã lưu DB thành công",
                device = fakeDeviceId,
                lang = langCode
            });
        }

        [HttpPost("init-first-day-tours")]
        public async Task<IActionResult> InitFirstDayTours()
        {
            if (!await _context.Tours.AnyAsync())
            {
                var newTours = new List<Tour>();
                for (int i = 1; i <= 10; i++)
                {
                    newTours.Add(new Tour
                    {
                        TourName = $"Hành trình di sản {i} - Đặc Biệt",
                        Description = $"Trải nghiệm chân thực ẩm thực quận 4 qua lộ trình {i}",
                        IsActive = true
                    });
                }
                _context.Tours.AddRange(newTours);
                await _context.SaveChangesAsync(); // Cần lưu trước để có ID

                // Phân bổ sạp vào 10 tour mới này
                var stalls = await _context.Stalls.Where(s => s.IsOpen).ToListAsync();
                if (stalls.Any())
                {
                    var rand = new Random();
                    foreach (var s in stalls)
                    {
                        s.TourID = newTours[rand.Next(newTours.Count)].Id;
                    }
                    await _context.SaveChangesAsync();
                }
                return Ok(new { message = "Đã sinh thành công 10 tour tức thời cho ngày đầu!" });
            }
            return Ok(new { message = "Tours đã tồn tại trong hệ thống." });
        }

        public class MockDataRequest { public int UserCount { get; set; } public int StallCount { get; set; } public int VisitCount { get; set; } }
    }
}