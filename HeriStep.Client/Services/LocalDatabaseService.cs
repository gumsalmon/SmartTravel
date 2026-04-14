using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using HeriStep.Client.Models.LocalModels;

namespace HeriStep.Client.Services
{
    public class LocalDatabaseService
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbPath;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public LocalDatabaseService()
        {
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HeriStepOffline.db3");
        }

        public async Task InitAsync()
        {
            // Double-check lock để tránh race condition khi gọi đồng thời
            if (_db != null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_db != null) return;

                _db = new SQLiteAsyncConnection(_dbPath,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create   |
                    SQLiteOpenFlags.SharedCache);

                // WAL mode: đọc/ghi đồng thời không lock nhau
                await _db.ExecuteAsync("PRAGMA journal_mode=WAL;");
                await _db.ExecuteAsync("PRAGMA synchronous=NORMAL;");

                await _db.CreateTableAsync<LocalStall>();
                await _db.CreateTableAsync<LocalTour>();

                // ─── BẢNG MỚI cho Heatmap ───────────────────────────────
                await _db.CreateTableAsync<StallVisit>();

                // Index tăng tốc GROUP BY StallId khi vẽ Heatmap
                await _db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_stallvisits_stallid " +
                    "ON StallVisits(StallId);");

                // Index phân tích giờ cao điểm: GROUP BY strftime('%H', VisitedAt)
                await _db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_stallvisits_visitedat " +
                    "ON StallVisits(VisitedAt);");
            }
            finally
            {
                _initLock.Release();
            }
        }

        #region Stall Operations

        public async Task<List<LocalStall>> GetStallsAsync()
        {
            try
            {
                await InitAsync();
                return await _db.Table<LocalStall>().ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCAL_DB] GetStallsAsync failed: {ex.Message}");
                return new List<LocalStall>();
            }
        }

        public async Task<LocalStall?> GetStallByIdAsync(int id)
        {
            try
            {
                await InitAsync();
                return await _db.Table<LocalStall>().FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCAL_DB] GetStallByIdAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<List<LocalMenuDish>> GetMenuItemsByStallIdAsync(int stallId)
        {
            try
            {
                await InitAsync();
                if (stallId <= 0) return new List<LocalMenuDish>();

                // Try dedicated local table first (if app has synced MenuItems)
                try
                {
                    var menuItems = await _db.QueryAsync<LocalMenuDish>(
                        @"SELECT Id,
                                 StallId,
                                 Name,
                                 Description,
                                 Price,
                                 ImageUrl
                          FROM MenuItems
                          WHERE StallId = ?
                          ORDER BY Id DESC",
                        stallId);
                    if (menuItems.Count > 0) return menuItems;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOCAL_DB] Query MenuItems failed: {ex.Message}");
                }

                // Fallback to Products/ProductTranslations schema if present in local DB
                try
                {
                    var products = await _db.QueryAsync<LocalMenuDish>(
                        @"SELECT p.id AS Id,
                                 p.stall_id AS StallId,
                                 COALESCE(pt.product_name, 'Món đặc trưng') AS Name,
                                 COALESCE(pt.short_description, '') AS Description,
                                 p.base_price AS Price,
                                 COALESCE(p.image_url, '') AS ImageUrl
                          FROM Products p
                          LEFT JOIN ProductTranslations pt ON pt.product_id = p.id
                          WHERE p.stall_id = ? AND (p.is_deleted = 0 OR p.is_deleted IS NULL)
                          ORDER BY p.is_signature DESC, p.id DESC",
                        stallId);
                    return products;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LOCAL_DB] Query Products fallback failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCAL_DB] GetMenuItemsByStallIdAsync failed: {ex.Message}");
            }

            return new List<LocalMenuDish>();
        }

        /// <summary>
        /// Tính năng: Xem Top 5 Quán (Luồng Offline-first)
        /// Truy vấn trực tiếp SQLite — không gọi API HTTP.
        /// SELECT TOP 5 FROM StallCache ORDER BY Rating DESC
        /// </summary>
        public async Task<List<LocalStall>> GetTop5StallsAsync()
        {
            await InitAsync();
            return await _db.Table<LocalStall>()
                            .OrderByDescending(s => s.Rating)
                            .Take(5)
                            .ToListAsync();
        }

        public async Task SaveStallsAsync(IEnumerable<LocalStall> stalls)
        {
            await InitAsync();
            foreach (var stall in stalls)
                await _db.InsertOrReplaceAsync(stall);
        }

        #endregion

        #region Tour Operations

        public async Task<List<LocalTour>> GetToursAsync()
        {
            await InitAsync();
            return await _db.Table<LocalTour>().ToListAsync();
        }

        /// <summary>
        /// Tính năng: Xem Top 10 Tour du lịch (Luồng Offline-first)
        /// SELECT * FROM TourCache WHERE IsActive=1 LIMIT 10
        /// </summary>
        public async Task<List<LocalTour>> GetTop10ToursAsync()
        {
            await InitAsync();
            return await _db.Table<LocalTour>()
                            .Where(t => t.IsActive)
                            .Take(10)
                            .ToListAsync();
        }

        public async Task SaveToursAsync(IEnumerable<LocalTour> tours)
        {
            await InitAsync();
            foreach (var tour in tours)
                await _db.InsertOrReplaceAsync(tour);
        }

        #endregion

        #region Free Exploration Mode — Legacy RAM helpers (kept for compat)

        /// <summary>
        /// [DEPRECATED — dùng GeofenceEngine.ResetVisitedFlags() thay thế]
        /// Reset toàn bộ cờ IsVisited = 0 trong DB.
        /// Giờ chỉ cần gọi khi muốn đồng bộ DB ↔ RAM sau restart app.
        /// </summary>
        public Task ResetAllVisitedAsync()
        {
            // IsVisited is no longer stored in DB ([SQLite.Ignore]), no-op
            return Task.CompletedTask;
        }

        public Task MarkStallVisitedAsync(int stallId)
        {
            // IsVisited is no longer stored in DB ([SQLite.Ignore]), no-op
            return Task.CompletedTask;
        }

        #endregion

        #region StallVisit (Heatmap Log)

        /// <summary>
        /// INSERT một lượt ghé thăm mới. Gọi từ GeofenceEngine qua Task.Run.
        /// Không bao giờ UPDATE StallCache — dữ liệu gốc được bảo vệ hoàn toàn.
        /// </summary>
        public async Task InsertStallVisitAsync(StallVisit visit)
        {
            await InitAsync();
            await _db.InsertAsync(visit);
        }

        /// <summary>
        /// Update listen duration for the latest visit of a specific stall
        /// </summary>
        public async Task UpdateLatestStallVisitDurationAsync(int stallId, int durationSeconds)
        {
            try
            {
                await InitAsync();
                var latestVisit = await _db.Table<StallVisit>()
                                           .Where(v => v.StallId == stallId)
                                           .OrderByDescending(v => v.VisitedAt)
                                           .FirstOrDefaultAsync();

                if (latestVisit != null)
                {
                    latestVisit.ListenDurationSeconds += durationSeconds;
                    await _db.UpdateAsync(latestVisit);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCAL_DB] UpdateLatestStallVisitDurationAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Dữ liệu Heatmap: đếm lượt ghé thăm theo từng sạp.
        /// SELECT StallId, StallName, COUNT(*) as VisitCount
        /// FROM StallVisits GROUP BY StallId ORDER BY VisitCount DESC
        /// </summary>
        public async Task<List<HeatmapEntry>> GetHeatmapDataAsync()
        {
            await InitAsync();
            return await _db.QueryAsync<HeatmapEntry>(
                @"SELECT StallId,
                         StallName,
                         COUNT(*)                           AS VisitCount,
                         MIN(VisitedAt)                     AS FirstVisit,
                         MAX(VisitedAt)                     AS LastVisit,
                         AVG(DistanceMeters)                AS AvgDistanceMeters
                  FROM   StallVisits
                  GROUP  BY StallId
                  ORDER  BY VisitCount DESC");
        }

        /// <summary>
        /// Phân tích giờ cao điểm: đếm lượt ghé theo khung giờ (0-23).
        /// SELECT strftime('%H', VisitedAt) as Hour, COUNT(*) as Visits
        /// </summary>
        public async Task<List<HourlyVisitEntry>> GetPeakHoursAsync(int stallId)
        {
            await InitAsync();
            return await _db.QueryAsync<HourlyVisitEntry>(
                @"SELECT CAST(strftime('%H', VisitedAt) AS INTEGER) AS Hour,
                         COUNT(*) AS Visits
                  FROM   StallVisits
                  WHERE  StallId = ?
                  GROUP  BY Hour
                  ORDER  BY Hour",
                stallId);
        }

        /// <summary>
        /// Lịch sử ghé thăm trong một phiên (để debug / review).
        /// </summary>
        public async Task<List<StallVisit>> GetVisitsBySessionAsync(string sessionId)
        {
            await InitAsync();
            return await _db.Table<StallVisit>()
                            .Where(v => v.SessionId == sessionId)
                            .OrderByDescending(v => v.VisitedAt)
                            .ToListAsync();
        }

        /// <summary>
        /// Tổng quan lượt ghé thực tế từ StallVisits.
        /// </summary>
        public async Task<ProfileVisitSummary> GetProfileVisitSummaryAsync()
        {
            await InitAsync();
            var result = await _db.QueryAsync<ProfileVisitSummary>(
                @"SELECT COUNT(*) AS TotalVisits,
                         COUNT(DISTINCT StallId) AS UniqueStalls
                  FROM StallVisits");
            return result.FirstOrDefault() ?? new ProfileVisitSummary();
        }

        /// <summary>
        /// Top quán đã ghé theo số lượt thực tế từ StallVisits.
        /// </summary>
        public async Task<List<RecentVisitedStall>> GetTopVisitedStallsAsync(int limit = 5)
        {
            await InitAsync();
            return await _db.QueryAsync<RecentVisitedStall>(
                @"SELECT v.StallId AS StallId,
                         MAX(v.StallName) AS StallName,
                         COUNT(*) AS VisitCount,
                         MAX(v.VisitedAt) AS LastVisitedAt,
                         COALESCE(MAX(s.ImageUrl), '') AS ImageUrl
                  FROM StallVisits v
                  LEFT JOIN StallCache s ON s.Id = v.StallId
                  GROUP BY v.StallId
                  ORDER BY VisitCount DESC, LastVisitedAt DESC
                  LIMIT ?",
                limit);
        }

        /// <summary>
        /// Xoá log cũ hơn N ngày (dọn dẹp định kỳ, gọi khi App khởi động).
        /// </summary>
        public async Task PurgeOldVisitsAsync(int olderThanDays = 90)
        {
            await InitAsync();
            var cutoff = DateTime.UtcNow.AddDays(-olderThanDays)
                                        .ToString("yyyy-MM-dd HH:mm:ss");
            await _db.ExecuteAsync(
                "DELETE FROM StallVisits WHERE VisitedAt < ?", cutoff);
        }

        #endregion
    }

    // ── Query result DTOs (không map vào bảng, chỉ dùng cho Query<>) ──

    /// <summary>Kết quả aggregate cho bản đồ Heatmap.</summary>
    public class HeatmapEntry
    {
        public int      StallId            { get; set; }
        public string   StallName          { get; set; } = string.Empty;
        public int      VisitCount         { get; set; }
        public DateTime FirstVisit         { get; set; }
        public DateTime LastVisit          { get; set; }
        public double   AvgDistanceMeters  { get; set; }
    }

    /// <summary>Kết quả phân tích giờ cao điểm.</summary>
    public class HourlyVisitEntry
    {
        public int Hour   { get; set; }   // 0–23
        public int Visits { get; set; }
    }

    public class ProfileVisitSummary
    {
        public int TotalVisits { get; set; }
        public int UniqueStalls { get; set; }
    }

    public class RecentVisitedStall
    {
        public int StallId { get; set; }
        public string StallName { get; set; } = string.Empty;
        public int VisitCount { get; set; }
        public DateTime LastVisitedAt { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class LocalMenuDish
    {
        public int Id { get; set; }
        public int StallId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
