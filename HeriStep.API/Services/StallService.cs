using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeriStep.API.Data;
using HeriStep.API.Interfaces;
using HeriStep.Shared.Models;
using HeriStep.Shared.Models.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HeriStep.API.Services
{
    public class StallService : IStallService
    {
        private readonly HeriStepDbContext _context;
        private readonly IDistributedCache _cache; // Redis Injection

        public StallService(HeriStepDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IEnumerable<PointOfInterest>> GetAllStallsAsync()
        {
            var cacheKey = "StallMapCache";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<IEnumerable<PointOfInterest>>(cachedData);
            }

            // DB Fallback if Cache MISS
            var query = from s in _context.Stalls
                        join u in _context.Users on s.OwnerId equals u.Id into userGroup
                        from user in userGroup.DefaultIfEmpty()
                        select new PointOfInterest
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Latitude = s.Latitude,
                            Longitude = s.Longitude,
                            IsOpen = s.IsOpen
                        };

            var result = await query.ToListAsync();

            // Lên Redis Server: Cache 10 mins
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);

            return result;
        }

        public async Task<bool> ExtendSubscriptionAsync(int stallId)
        {
            var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StallId == stallId);
            if (sub != null)
            {
                sub.ExpiryDate = sub.ExpiryDate < DateTime.Now ? DateTime.Now.AddDays(30) : sub.ExpiryDate.Value.AddDays(30);
                sub.IsActive = true;
            }
            else
            {
                _context.Subscriptions.Add(new Subscription
                {
                    StallId = stallId,
                    StartDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(30),
                    IsActive = true
                });
            }
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
