using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using HeriStep.Client.Models.LocalModels;

namespace HeriStep.Client.Services
{
    public class LocalDatabaseService
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbPath;

        public LocalDatabaseService()
        {
            _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeriStepOffline.db3");
        }

        public async Task InitAsync()
        {
            if (_db != null)
                return;

            _db = new SQLiteAsyncConnection(_dbPath);

            // Create tables if they do not exist
            await _db.CreateTableAsync<LocalStall>();
            await _db.CreateTableAsync<LocalTour>();
        }

        #region Stall Operations

        public async Task<List<LocalStall>> GetStallsAsync()
        {
            await InitAsync();
            return await _db.Table<LocalStall>().ToListAsync();
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
            
            // Replaces if ID already exists, otherwise inserts
            foreach (var stall in stalls)
            {
                await _db.InsertOrReplaceAsync(stall);
            }
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
        /// Không gọi API — render trực tiếp từ SQLite nội bộ.
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
            {
                await _db.InsertOrReplaceAsync(tour);
            }
        }

        #endregion

        #region Free Exploration Mode

        /// <summary>
        /// Đánh dấu một quán đã được phát TTS (IsVisited = true).
        /// Chống vòng lặp đọc lại theo sơ đồ sequence.
        /// </summary>
        public async Task MarkStallVisitedAsync(int stallId)
        {
            await InitAsync();
            var stall = await _db.Table<LocalStall>().Where(s => s.Id == stallId).FirstOrDefaultAsync();
            if (stall != null)
            {
                stall.IsVisited = true;
                await _db.UpdateAsync(stall);
            }
        }

        /// <summary>
        /// Kiểm tra xem quán đã được phát TTS chưa.
        /// </summary>
        public async Task<bool> IsStallVisitedAsync(int stallId)
        {
            await InitAsync();
            var stall = await _db.Table<LocalStall>().Where(s => s.Id == stallId).FirstOrDefaultAsync();
            return stall?.IsVisited ?? false;
        }

        /// <summary>
        /// Reset toàn bộ cờ IsVisited = false.
        /// Gọi khi User bật lại chế độ Khám Phá Tự Do (bắt đầu lại phiên mới).
        /// </summary>
        public async Task ResetAllVisitedAsync()
        {
            await InitAsync();
            await _db.ExecuteAsync("UPDATE StallCache SET IsVisited = 0");
        }

        #endregion
    }
}
