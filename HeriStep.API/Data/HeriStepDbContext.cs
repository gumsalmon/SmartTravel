using HeriStep.Shared; // Để nhận diện được PointOfInterest và Tour
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Data
{
    public class HeriStepDbContext : DbContext
    {
        public HeriStepDbContext(DbContextOptions<HeriStepDbContext> options) : base(options)
        {
        }

        // Khai báo 2 bảng dữ liệu chính
        public DbSet<PointOfInterest> Points { get; set; }
        public DbSet<Tour> Tours { get; set; }
    }
}