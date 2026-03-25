using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net; // 💡 Đã thêm thư viện BCrypt
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

        // ĐÃ XÓA HÀM HASHPASSWORD CŨ (SHA-256) ĐI RỒI!

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateMockData([FromBody] MockDataRequest req)
        {
            var rand = new Random();
            var now = DateTime.Now;

            try
            {
                // 1. DỌN RÁC CŨ
                _context.StallVisits.RemoveRange(await _context.StallVisits.ToListAsync());
                _context.TouristTickets.RemoveRange(await _context.TouristTickets.ToListAsync());
                _context.Subscriptions.RemoveRange(await _context.Subscriptions.ToListAsync());
                _context.Stalls.RemoveRange(await _context.Stalls.ToListAsync());

                var oldStallOwners = await _context.Users.Where(u => u.Role == "StallOwner").ToListAsync();
                _context.Users.RemoveRange(oldStallOwners);
                await _context.SaveChangesAsync();

                // 2. TẠO TOURS & USERS (CHUẨN BCRYPT)
                var tours = await _context.Tours.ToListAsync();
                if (!tours.Any())
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        _context.Tours.Add(new Tour { TourName = $"Hành trình di sản {i}", Description = "Mô tả tour", IsActive = true });
                    }
                    await _context.SaveChangesAsync();
                    tours = await _context.Tours.ToListAsync();
                }

                var createdUsers = new List<User>();

                // 💡 ĐÃ ĐỒNG BỘ: Sử dụng chuẩn BCrypt giống hệt bên AuthController!
                string defaultHashedPassword = BCrypt.Net.BCrypt.HashPassword("123456");

                for (int i = 0; i < req.UserCount; i++)
                {
                    var user = new User
                    {
                        Username = $"user_{Guid.NewGuid().ToString("N").Substring(0, 5)}",
                        PasswordHash = defaultHashedPassword, // Gán cục BCrypt vào đây
                        FullName = $"Chủ Sạp {i + 1}",
                        Role = "StallOwner"
                    };
                    _context.Users.Add(user);
                    createdUsers.Add(user);
                }
                await _context.SaveChangesAsync();

                // 3. TẠO SẠP (Rải rác trong mặt phẳng)
                var createdStalls = new List<Stall>();
                double minLat = 10.7585, maxLat = 10.7631;
                double minLng = 106.7015, maxLng = 106.7050;

                for (int i = 0; i < req.StallCount; i++)
                {
                    double randomLat = minLat + rand.NextDouble() * (maxLat - minLat);
                    double randomLng = minLng + rand.NextDouble() * (maxLng - minLng);

                    var stall = new Stall
                    {
                        Name = $"Sạp hàng {rand.Next(100, 999)}",
                        Latitude = randomLat,
                        Longitude = randomLng,
                        IsOpen = true,
                        RadiusMeter = 20,
                        OwnerId = createdUsers[rand.Next(createdUsers.Count)].Id,
                        TourID = tours[rand.Next(tours.Count)].Id
                    };
                    _context.Stalls.Add(stall);
                    createdStalls.Add(stall);
                }
                await _context.SaveChangesAsync();

                // 4. TẠO SUBSCRIPTION (Gói cước chủ sạp)
                foreach (var stall in createdStalls)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));
                    _context.Subscriptions.Add(new Subscription
                    {
                        StallId = stall.Id,
                        DeviceId = $"HS-DEV-{rand.Next(1000, 9999)}",
                        ActivationCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        StartDate = randomDate,
                        ExpiryDate = randomDate.AddDays(30),
                        IsActive = true
                    });
                }
                await _context.SaveChangesAsync();

                // 5. TẠO 3 GÓI VÉ DU LỊCH & LƯỢT KHÁCH
                var packages = await _context.TicketPackages.ToListAsync();
                if (!packages.Any())
                {
                    packages = new List<TicketPackage>
                    {
                        new TicketPackage { PackageName = "Vé 3 Ngày Vĩnh Khánh", Price = 50000, DurationHours = 72, IsActive = true },
                        new TicketPackage { PackageName = "Vé 5 Ngày Vĩnh Khánh", Price = 100000, DurationHours = 120, IsActive = true },
                        new TicketPackage { PackageName = "Vé Tuần Vĩnh Khánh", Price = 150000, DurationHours = 168, IsActive = true }
                    };
                    _context.TicketPackages.AddRange(packages);
                    await _context.SaveChangesAsync();
                }

                for (int i = 0; i < req.VisitCount; i++)
                {
                    var randomDate = now.AddDays(-rand.Next(0, 90));
                    var randomPackage = packages[rand.Next(packages.Count)];

                    _context.TouristTickets.Add(new TouristTicket
                    {
                        TicketCode = $"TC-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                        DeviceId = $"DEV-{rand.Next(1000, 9999)}",
                        PackageId = randomPackage.Id,
                        AmountPaid = randomPackage.Price,
                        CreatedAt = randomDate,
                        ExpiryDate = randomDate.AddHours(randomPackage.DurationHours)
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
                return Ok(new { message = "Mock data thành công! Đã đồng bộ BCrypt 100%!" });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { error = "Lỗi khi lưu DB", detail = msg });
            }
        }

        // ==========================================
        // 🧪 MOCK TEST: GIẢ LẬP KHÁCH VÀO SẠP (TẠO DATA ĐỘNG)
        // ==========================================
        [HttpPost("simulate-tourist/{stallId}/{langCode}")]
        public async Task<IActionResult> SimulateTourist(int stallId, string langCode, [FromQuery] string? deviceId)
        {
            var stall = await _context.Stalls.FindAsync(stallId);
            if (stall == null) return NotFound(new { message = "Không tìm thấy sạp" });

            string finalDeviceId = string.IsNullOrEmpty(deviceId)
                ? $"MOCK-APP-{langCode.ToUpper()}-{Guid.NewGuid().ToString().Substring(0, 6)}"
                : deviceId;

            var visit = new StallVisit
            {
                StallId = stallId,
                DeviceId = finalDeviceId,
                VisitedAt = DateTime.Now
            };

            _context.StallVisits.Add(visit);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã lưu DB thành công",
                device = finalDeviceId,
                lang = langCode
            });
        }

        public class MockDataRequest { public int UserCount { get; set; } public int StallCount { get; set; } public int VisitCount { get; set; } }
    }
}