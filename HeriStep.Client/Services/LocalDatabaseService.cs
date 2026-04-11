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

        public async Task SaveToursAsync(IEnumerable<LocalTour> tours)
        {
            await InitAsync();
            
            foreach (var tour in tours)
            {
                await _db.InsertOrReplaceAsync(tour);
            }
        }

        #endregion
    }
}
