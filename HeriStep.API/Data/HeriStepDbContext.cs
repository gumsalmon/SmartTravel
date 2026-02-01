using HeriStep.Shared;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Data
{
    public class HeriStepDbContext : DbContext
    {
        public HeriStepDbContext(DbContextOptions<HeriStepDbContext> options) : base(options)
        {
        }

        // Khai báo bảng cho các điểm dừng (Sạp hàng)
        public DbSet<PointOfInterest> Stalls { get; set; }

        // Khai báo bảng cho các Chuyến tham quan
        public DbSet<Tour> Tours { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đảm bảo không còn bất kỳ dòng Ignore("TourId") nào ở đây
        }
    }
}