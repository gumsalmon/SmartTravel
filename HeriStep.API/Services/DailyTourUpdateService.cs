using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Services
{
    public class DailyTourUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyTourUpdateService> _logger;

        public DailyTourUpdateService(IServiceProvider serviceProvider, ILogger<DailyTourUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Bot cập nhật Tour Hot đã khởi động...");

            // 💡 ÁO GIÁP NGOÀI CÙNG: Bọc toàn bộ vòng lặp để bắt lỗi khi tắt Server
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var now = DateTime.Now;
                    var nextMidnight = DateTime.Today.AddDays(1);
                    var timeToWait = nextMidnight - now;

                    // ==========================================
                    // 💡 THỦ THUẬT DEMO ĐỒ ÁN (CỰC QUAN TRỌNG)
                    timeToWait = TimeSpan.FromSeconds(15);
                    // ==========================================

                    _logger.LogInformation($"⏰ Sẽ cập nhật bảng xếp hạng Tour sau: {timeToWait.TotalMinutes:F1} phút nữa.");

                    // Khi bấm Stop Server, dòng Delay này sẽ bị hủy và quăng lỗi TaskCanceledException
                    await Task.Delay(timeToWait, stoppingToken);

                    _logger.LogInformation("🔄 Bắt đầu quét dữ liệu và cập nhật TOP 10 Tour...");

                    // Try-catch bên trong để xử lý lỗi Database (nếu có) mà không làm sập tiến trình ngầm
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<HeriStepDbContext>();
                            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

                            // ==========================================
                            // --- BƯỚC 0: ĐẢM BẢO LUÔN CÓ ĐỦ 10 LỘ TRÌNH VÀ GÁN SẠP NGẪU NHIÊN ---
                            // ==========================================
                            var currentTourCount = await context.Tours.CountAsync(stoppingToken);
                            if (currentTourCount < 10)
                            {
                                int toursToCreate = 10 - currentTourCount;
                                var newTours = new List<Tour>(); // Lưu tạm để tí nữa nhét sạp vào

                                for (int i = 0; i < toursToCreate; i++)
                                {
                                    var tour = new Tour
                                    {
                                        TourName = $"Lộ trình khám phá Vĩnh Khánh {i + 1}",
                                        Description = "Lộ trình hệ thống tự động sinh ra để phục vụ du khách trải nghiệm.",
                                        IsActive = true,
                                        IsTopHot = false
                                    };
                                    context.Tours.Add(tour);
                                    newTours.Add(tour);
                                }
                                await context.SaveChangesAsync(stoppingToken); // Lưu để lấy ID Lộ trình

                                // 💡 Bốc ngẫu nhiên các sạp có sẵn nhét vào các lộ trình vừa tạo
                                var allStalls = await context.Stalls.ToListAsync(stoppingToken);
                                if (allStalls.Any())
                                {
                                    var rand = new Random();
                                    foreach (var tour in newTours)
                                    {
                                        // Chọn ngẫu nhiên từ 2 đến 5 quán
                                        int numStalls = rand.Next(2, 6);
                                        var randomStalls = allStalls.OrderBy(x => Guid.NewGuid()).Take(numStalls).ToList();

                                        foreach (var stall in randomStalls)
                                        {
                                            stall.TourID = tour.Id;
                                            context.Stalls.Update(stall);
                                        }
                                    }
                                    await context.SaveChangesAsync(stoppingToken);
                                }

                                _logger.LogInformation($"🛠 Đã tạo {toursToCreate} Lộ trình mới và nhét ngẫu nhiên các quán vào!");
                            }

                            // --- BƯỚC 1: RESET TRẠNG THÁI CŨ ---
                            var currentHotTours = await context.Tours.Where(t => t.IsTopHot == true).ToListAsync(stoppingToken);
                            foreach (var t in currentHotTours)
                            {
                                t.IsTopHot = false;
                            }

                            // --- BƯỚC 2: TÍNH TOÁN TOP 10 MỚI (CÓ XỬ LÝ NGÀY ĐẦU TIÊN) ---
                            var top10TourIds = await context.Tours
                                .Where(t => t.IsActive == true)
                                .Select(t => new
                                {
                                    TourId = t.Id,
                                    TotalVisits = context.Stalls
                                        .Where(s => s.TourID == t.Id)
                                        .SelectMany(s => context.StallVisits.Where(v => v.StallId == s.Id && v.VisitedAt >= thirtyDaysAgo))
                                        .Count()
                                })
                                .OrderByDescending(x => x.TotalVisits)
                                .ThenBy(x => Guid.NewGuid())
                                .Take(10)
                                .Select(x => x.TourId)
                                .ToListAsync(stoppingToken);

                            // --- BƯỚC 3: CẬP NHẬT KẾT QUẢ MỚI ---
                            if (top10TourIds.Any())
                            {
                                var newHotTours = await context.Tours.Where(t => top10TourIds.Contains(t.Id)).ToListAsync(stoppingToken);
                                foreach (var t in newHotTours)
                                {
                                    t.IsTopHot = true;
                                }

                                await context.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation($"✅ Đã cập nhật thành công {newHotTours.Count} Tour lên Top Hot!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Lỗi khi cập nhật Tour Hot: {ex.Message}");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // 💡 NƠI HẠ CÁNH AN TOÀN: Khi sếp bấm Stop, code nhảy vào đây và tự kết thúc êm đẹp.
                _logger.LogInformation("🛑 [Hệ thống] Bot cập nhật Tour đã nhận lệnh dừng và đang tắt an toàn...");
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"🚨 [Lỗi Nghiêm Trọng] Bot sập nguồn: {ex.Message}");
            }
        }
    }
}