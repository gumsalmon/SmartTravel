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
            _logger.LogInformation("🚀 Bot cập nhật Tour Hot đã khởi động và đang đợi đến 0h00...");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Tính toán thời gian nghỉ đến 0h00 sáng mai
                var now = DateTime.Now;
                var nextMidnight = DateTime.Today.AddDays(1);
                var timeToWait = nextMidnight - now;

                // Tạm dừng Bot cho đến nửa đêm
                await Task.Delay(timeToWait, stoppingToken);

                _logger.LogInformation("⏰ Đã đến 0h00! Đang tiến hành quét dữ liệu và cập nhật bảng xếp hạng Tour...");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<HeriStepDbContext>();
                        var thirtyDaysAgo = DateTime.Now.AddDays(-30);

                        // --- BƯỚC 1: RESET TRẠNG THÁI ---
                        // Đưa tất cả các Tour về trạng thái bình thường (IsTopHot = false)
                        var currentHotTours = await context.Tours.Where(t => t.IsTopHot == true).ToListAsync();
                        foreach (var t in currentHotTours)
                        {
                            t.IsTopHot = false;
                        }

                        // --- BƯỚC 2: TÍNH TOÁN TOP 10 MỚI ---
                        // Thuật toán: Đếm tổng lượt ghé thăm (StallVisits) của tất cả các sạp thuộc Tour đó trong 30 ngày qua
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
                            .Take(10)
                            .Select(x => x.TourId)
                            .ToListAsync();

                        // --- BƯỚC 3: CẬP NHẬT KẾT QUẢ ---
                        if (top10TourIds.Any())
                        {
                            var newHotTours = await context.Tours.Where(t => top10TourIds.Contains(t.Id)).ToListAsync();
                            foreach (var t in newHotTours)
                            {
                                t.IsTopHot = true;
                            }

                            await context.SaveChangesAsync();
                            _logger.LogInformation($"✅ Đã cập nhật thành công {newHotTours.Count} Tour lên Top Hot.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Lỗi khi cập nhật Tour Hot: {ex.Message}");
                }
            }
        }
    }
}