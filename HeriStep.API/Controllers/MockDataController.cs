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

        // 💡 LỚP PHỤ TRỢ: Định nghĩa các đoạn đường/hẻm
        private class StreetSegment
        {
            public double StartLat { get; set; }
            public double StartLng { get; set; }
            public double EndLat { get; set; }
            public double EndLng { get; set; }
            public StreetSegment(double sl, double slg, double el, double elg)
            {
                StartLat = sl; StartLng = slg; EndLat = el; EndLng = elg;
            }
        }

        // 💡 HÀM PHỤ TRỢ: Tính khoảng cách giữa 2 điểm (để chống trùng/đè lên nhau)
        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2));
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
                    for (int i = 1; i <= 10; i++)
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
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                        FullName = $"Chủ Sạp {i + 1}",
                        Role = "StallOwner"
                    };
                    _context.Users.Add(user);
                    createdUsers.Add(user);
                }
                await _context.SaveChangesAsync();

                // 3. TẠO SẠP THEO MẠNG LƯỚI ĐƯỜNG & HẺM (BÁM SÁT ẢNH BẢN ĐỒ CỦA SẾP)
                var createdStalls = new List<Stall>();

                // Dựng mạng lưới các đường line xanh lam (Đường chính + các hẻm xương cá)
                var streetNetwork = new List<StreetSegment>
                {
                    // Trục chính Vĩnh Khánh (Cong cong)
                    new StreetSegment(10.76210, 106.70130, 10.76070, 106.70320),
                    new StreetSegment(10.76070, 106.70320, 10.75930, 106.70250),
                    new StreetSegment(10.75930, 106.70250, 10.75810, 106.70210),
                    // Các hẻm nhánh đâm ra Hoàng Diệu và Đoàn Văn Bơ
                    new StreetSegment(10.76070, 106.70320, 10.76180, 106.70450),
                    new StreetSegment(10.75930, 106.70250, 10.75850, 106.70400),
                    // Mép đường Hoàng Diệu
                    new StreetSegment(10.76210, 106.70130, 10.76350, 106.70380),
                    // Các đường hẻm đan xen khu vực giữa (Tạo mạng nhện xanh lam)
                    new StreetSegment(10.76180, 106.70450, 10.75930, 106.70550),
                    new StreetSegment(10.76000, 106.70400, 10.75850, 106.70500),
                    new StreetSegment(10.76250, 106.70250, 10.76100, 106.70400)
                };

                for (int i = 0; i < req.StallCount; i++)
                {
                    double targetLat = 0, targetLng = 0;
                    bool isValidPlace = false;
                    int maxRetries = 50; // Thử tối đa 50 lần để tìm chỗ trống không bị trùng

                    while (!isValidPlace && maxRetries > 0)
                    {
                        maxRetries--;
                        // Bốc ngẫu nhiên 1 con đường/hẻm trong mạng lưới
                        var seg = streetNetwork[rand.Next(streetNetwork.Count)];

                        // Chọn ngẫu nhiên 1 điểm NẰM TRÊN con đường đó
                        double t = rand.NextDouble();
                        targetLat = seg.StartLat + t * (seg.EndLat - seg.StartLat);
                        targetLng = seg.StartLng + t * (seg.EndLng - seg.StartLng);

                        // Lắc nhẹ tọa độ để sạp dạt ra vỉa hè 2 bên đường (tầm 3-5 mét)
                        targetLat += (rand.NextDouble() - 0.5) * 0.00008;
                        targetLng += (rand.NextDouble() - 0.5) * 0.00008;

                        isValidPlace = true;
                        // 💡 THUẬT TOÁN CHỐNG TRÙNG: Kiểm tra xem chỗ này có sạp nào chiếm chưa?
                        foreach (var existingStall in createdStalls)
                        {
                            // 0.0001 độ tương đương khoảng 10 mét. 
                            if (GetDistance(targetLat, targetLng, existingStall.Latitude, existingStall.Longitude) < 0.0001)
                            {
                                isValidPlace = false; // Quá gần nhau -> Vứt, quay lại tìm chỗ khác!
                                break;
                            }
                        }
                    }

                    var stall = new Stall
                    {
                        Name = $"Sạp hàng {rand.Next(100, 999)}",
                        Latitude = targetLat,
                        Longitude = targetLng,
                        IsOpen = true,
                        RadiusMeter = 50,
                        OwnerId = createdUsers[rand.Next(createdUsers.Count)].Id,
                        TourID = tours[rand.Next(tours.Count)].Id
                    };
                    _context.Stalls.Add(stall);
                    createdStalls.Add(stall);
                }

                // Lưu DB
                await _context.SaveChangesAsync();

                // 4. Tạo Subscription
                foreach (var stall in createdStalls)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));
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

                // Self-Healing sạp mồ côi
                var orphanStalls = await _context.Stalls.Where(s => s.IsOpen && (s.TourID == null || s.TourID == 0)).ToListAsync();
                if (orphanStalls.Any() && tours.Any())
                {
                    foreach (var s in orphanStalls) s.TourID = tours[rand.Next(tours.Count)].Id;
                    await _context.SaveChangesAsync();
                }

                // 5. Tạo Package & Lịch sử mua vé
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
                return Ok(new { message = "Mock data đã rải đều trên đường và hẻm, chống đè 100%!" });
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

            var visit = new StallVisit { StallId = stallId, DeviceId = fakeDeviceId, VisitedAt = DateTime.Now };
            _context.StallVisits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã lưu DB", device = fakeDeviceId, lang = langCode });
        }

        [HttpPost("init-first-day-tours")]
        public async Task<IActionResult> InitFirstDayTours()
        {
            if (!await _context.Tours.AnyAsync())
            {
                var newTours = new List<Tour>();
                for (int i = 1; i <= 10; i++) newTours.Add(new Tour { TourName = $"Hành trình di sản {i}", Description = "Demo", IsActive = true });
                _context.Tours.AddRange(newTours);
                await _context.SaveChangesAsync();

                var stalls = await _context.Stalls.Where(s => s.IsOpen).ToListAsync();
                if (stalls.Any())
                {
                    var rand = new Random();
                    foreach (var s in stalls) s.TourID = newTours[rand.Next(newTours.Count)].Id;
                    await _context.SaveChangesAsync();
                }
                return Ok(new { message = "Đã sinh tour ban đầu!" });
            }
            return Ok(new { message = "Tours đã tồn tại." });
        }

        public class MockDataRequest { public int UserCount { get; set; } public int StallCount { get; set; } public int VisitCount { get; set; } }
    }
}