using HeriStep.Shared;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Data
{
    public class HeriStepDbContext : DbContext
    {
        public HeriStepDbContext(DbContextOptions<HeriStepDbContext> options) : base(options)
        {
        }

        public DbSet<PointOfInterest> Stalls { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<StallContent> StallContents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình bảng Users
            modelBuilder.Entity<User>(entity => {
                entity.HasIndex(u => u.Username).IsUnique();
            });

            // 2. Cấu hình bảng Tours (SỬA LỖI SQL EXCEPTION)
            modelBuilder.Entity<Tour>(entity => {
                entity.ToTable("Tours"); // Khớp với tên bảng trong SQL

                // Ánh xạ chính xác tên cột từ PascalCase (C#) sang snake_case (SQL)
                entity.Property(t => t.TourName).HasColumnName("tour_name");
                entity.Property(t => t.ImageUrl).HasColumnName("image_url");
                entity.Property(t => t.IsActive).HasColumnName("is_active");
            });

            // 3. Cấu hình bảng PointOfInterest (Sạp hàng)
            modelBuilder.Entity<PointOfInterest>(entity => {
                entity.ToTable("Stalls");
                entity.Property(p => p.TourID).HasColumnName("TourID");

                // Báo cho EF bỏ qua cột này vì nó nằm ở bảng StallContents
                entity.Ignore(p => p.TtsScript);
            });

            // 4. Cấu hình bảng StallContents
            modelBuilder.Entity<StallContent>(entity => {
                entity.ToTable("StallContents");
                entity.Property(c => c.StallId).HasColumnName("stall_id");
                entity.Property(c => c.LangCode).HasColumnName("lang_code");
                entity.Property(c => c.TtsScript).HasColumnName("tts_script");
            });

            // 5. Cấu hình bảng Subscriptions
            modelBuilder.Entity<Subscription>(entity => {
                entity.ToTable("Subscriptions");
                entity.Property(s => s.StallId).HasColumnName("stall_id");
            });

            // 6. Cấu hình bảng Product
            modelBuilder.Entity<Product>(entity => {
                entity.ToTable("Products");
                entity.Property(p => p.BasePrice).HasColumnType("decimal(18,2)");
            });
        }
    }
}