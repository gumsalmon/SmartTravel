using Microsoft.EntityFrameworkCore;
using HeriStep.Shared; // Để nó hiểu class Stall nằm ở đâu

namespace HeriStep.API.Data
{
    public class HeriStepDbContext : DbContext
    {
        public HeriStepDbContext(DbContextOptions<HeriStepDbContext> options) : base(options) { }

        // Khai báo: "Tôi quản lý bảng Stalls trong SQL"
        public DbSet<Stall> Stalls { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<StallContent> StallContents { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

    }
}