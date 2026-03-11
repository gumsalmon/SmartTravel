using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Data
{
    public class HeriStepDbContext : DbContext
    {
        public HeriStepDbContext(DbContextOptions<HeriStepDbContext> options) : base(options) { }

        public DbSet<PointOfInterest> Stalls { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<StallContent> StallContents { get; set; }
        public DbSet<Language> Languages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình bảng Users
            modelBuilder.Entity<User>(entity => {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Username).HasColumnName("username");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.FullName).HasColumnName("full_name");
                entity.Property(u => u.Role).HasColumnName("role");
            });

            // 2. Cấu hình bảng Tours (ĐÃ SỬA LỖI IMAGEURL TẠI ĐÂY)
            modelBuilder.Entity<Tour>(entity => {
                entity.ToTable("Tours");
                entity.Property(t => t.TourName).HasColumnName("tour_name");

                // 💡 Dòng quan trọng nhất để hết lỗi 'Invalid column name ImageUrl'
                entity.Property(t => t.ImageUrl).HasColumnName("image_url");
                entity.Property(t => t.Description).HasColumnName("description");

                entity.Property(t => t.IsActive).HasColumnName("is_active");
            });

            // 3. Cấu hình bảng PointOfInterest (Stalls)
            modelBuilder.Entity<PointOfInterest>(entity => {
                entity.ToTable("Stalls");
                entity.Property(p => p.Name).HasColumnName("name_default");
                entity.Property(p => p.OwnerId).HasColumnName("owner_id");

                // 💡 Ánh xạ chính xác TourID để liên kết lộ trình
                entity.Property(p => p.TourID).HasColumnName("TourID");

                entity.Property(p => p.RadiusMeter).HasColumnName("radius_meter");
                entity.Property(p => p.IsOpen).HasColumnName("is_open");
                entity.Property(p => p.ImageUrl).HasColumnName("image_thumb");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

                entity.Ignore(p => p.TtsScript);
            });

            // 4. Cấu hình bảng StallContents
            modelBuilder.Entity<StallContent>(entity => {
                entity.ToTable("StallContents");
                entity.Property(c => c.StallId).HasColumnName("stall_id");
                entity.Property(c => c.LangCode).HasColumnName("lang_code");
                entity.Property(c => c.TtsScript).HasColumnName("tts_script");
                entity.Property(c => c.IsActive).HasColumnName("is_active");
            });

            // 5. Cấu hình bảng Subscriptions
            modelBuilder.Entity<Subscription>(entity => {
                entity.ToTable("Subscriptions");
                entity.Property(s => s.DeviceId).HasColumnName("device_id");
                entity.Property(s => s.ActivationCode).HasColumnName("activation_code");
                entity.Property(s => s.StartDate).HasColumnName("start_date");
                entity.Property(s => s.ExpiryDate).HasColumnName("expiry_date");
                entity.Property(s => s.IsActive).HasColumnName("is_active");
            });

            // 6. Cấu hình bảng Languages
            modelBuilder.Entity<Language>(entity => {
                entity.ToTable("Languages");
                entity.HasKey(l => l.LangCode);
                entity.Property(l => l.LangCode).HasColumnName("lang_code");
                entity.Property(l => l.LangName).HasColumnName("lang_name");
            });

            // 7. Cấu hình bảng Products
            modelBuilder.Entity<Product>(entity => {
                entity.ToTable("Products");
                entity.Property(p => p.BasePrice).HasColumnType("decimal(18,2)").HasColumnName("base_price");
                entity.Property(p => p.ImageUrl).HasColumnName("image_url");
                entity.Property(p => p.IsSignature).HasColumnName("is_signature");
            });
        }
    }
}