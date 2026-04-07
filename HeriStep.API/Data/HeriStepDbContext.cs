using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Data
{
    public class HeriStepDbContext : DbContext
    {
        public HeriStepDbContext(DbContextOptions<HeriStepDbContext> options) : base(options) { }

        public DbSet<Stall> Stalls { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<StallContent> StallContents { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<StallVisit> StallVisits { get; set; }
        public DbSet<Product> Products { get; set; } // Giữ lại 1 cái thôi
        public DbSet<ProductTranslation> ProductTranslations { get; set; }

        // Bảng cho Khách du lịch
        public DbSet<TicketPackage> TicketPackages { get; set; }
        public DbSet<TouristTicket> TouristTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Cấu hình bảng Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Username).HasColumnName("username");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.FullName).HasColumnName("full_name");
                entity.Property(u => u.Role).HasColumnName("role");
            });

            // 2. Cấu hình bảng Tours
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.ToTable("Tours");
                entity.Property(t => t.TourName).HasColumnName("tour_name");
                entity.Property(t => t.ImageUrl).HasColumnName("image_url");
                entity.Property(t => t.Description).HasColumnName("description");
                entity.Property(t => t.IsActive).HasColumnName("is_active");
                entity.Property(t => t.IsTopHot).HasColumnName("is_top_hot");
            });

            // 3. Cấu hình bảng Stall (Stalls)
            modelBuilder.Entity<Stall>(entity =>
            {
                // Fix lỗi Trigger cho EF Core 7+
                entity.ToTable("Stalls", tb => tb.HasTrigger("SomeTriggerName"));

                entity.Property(p => p.Name).HasColumnName("name_default");
                entity.Property(p => p.OwnerId).HasColumnName("owner_id");
                entity.Property(p => p.SortOrder).HasColumnName("sort_order");
                entity.Property(p => p.TourID).HasColumnName("TourID");
                entity.Property(p => p.RadiusMeter).HasColumnName("radius_meter");
                entity.Property(p => p.IsOpen).HasColumnName("is_open");
                entity.Property(p => p.ImageUrl).HasColumnName("image_thumb");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

                entity.Ignore(p => p.TtsScript);
                entity.Ignore(p => p.OwnerName); // Tránh lỗi EF cố tìm cột ảo này
            });

            // 4. Cấu hình bảng StallContents
            modelBuilder.Entity<StallContent>(entity =>
            {
                entity.ToTable("StallContents");
                entity.Property(c => c.StallId).HasColumnName("stall_id");
                entity.Property(c => c.LangCode).HasColumnName("lang_code");
                entity.Property(c => c.TtsScript).HasColumnName("tts_script");
                entity.Property(c => c.IsActive).HasColumnName("is_active");
            });

            // 5. Cấu hình bảng Subscriptions
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.ToTable("Subscriptions");
                entity.Property(s => s.StallId).HasColumnName("stall_id");
                entity.Property(s => s.DeviceId).HasColumnName("device_id");
                entity.Property(s => s.ActivationCode).HasColumnName("activation_code");
                entity.Property(s => s.StartDate).HasColumnName("start_date");
                entity.Property(s => s.ExpiryDate).HasColumnName("expiry_date");
                entity.Property(s => s.IsActive).HasColumnName("is_active");
            });

            // 6. Cấu hình bảng Languages
            modelBuilder.Entity<Language>(entity =>
            {
                entity.ToTable("Languages");
                entity.HasKey(l => l.LangCode);
                entity.Property(l => l.LangCode).HasColumnName("lang_code");
                entity.Property(l => l.LangName).HasColumnName("lang_name");
            });

            // 7. Cấu hình bảng Products
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.Property(p => p.StallId).HasColumnName("stall_id");
                entity.Property(p => p.BasePrice).HasColumnType("decimal(18,2)").HasColumnName("base_price");
                entity.Property(p => p.ImageUrl).HasColumnName("image_url");
                entity.Property(p => p.IsSignature).HasColumnName("is_signature");
            });

            // 8. Cấu hình bảng StallVisits
            modelBuilder.Entity<StallVisit>(entity =>
            {
                entity.ToTable("StallVisits");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.StallId).HasColumnName("stall_id");
                entity.Property(v => v.DeviceId).HasColumnName("device_id");
                entity.Property(v => v.VisitedAt).HasColumnName("visited_at");
            });

            // 💡 ĐÃ THÊM: 9. Cấu hình bảng TicketPackages
            modelBuilder.Entity<TicketPackage>(entity =>
            {
                entity.ToTable("TicketPackages");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.PackageName).HasColumnName("package_name");
                entity.Property(t => t.Price).HasColumnType("decimal(18,2)").HasColumnName("price");
                entity.Property(t => t.DurationHours).HasColumnName("duration_hours");
                entity.Property(t => t.IsActive).HasColumnName("is_active");
            });

            // 💡 ĐÃ THÊM: 10. Cấu hình bảng TouristTickets
            modelBuilder.Entity<TouristTicket>(entity =>
            {
                entity.ToTable("TouristTickets");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.TicketCode).HasColumnName("ticket_code");
                entity.Property(t => t.DeviceId).HasColumnName("device_id");
                entity.Property(t => t.PackageId).HasColumnName("package_id");
                entity.Property(t => t.AmountPaid).HasColumnType("decimal(18,2)").HasColumnName("amount_paid");
                entity.Property(t => t.PaymentMethod).HasColumnName("payment_method");
                entity.Property(t => t.CreatedAt).HasColumnName("created_at");
                entity.Property(t => t.ExpiryDate).HasColumnName("expiry_date");

                entity.Ignore(t => t.PackageName); // Bỏ qua biến ảo để ghép tên
            });
            // 11. Cấu hình bảng ProductTranslations
            modelBuilder.Entity<ProductTranslation>(entity =>
            {
                entity.ToTable("ProductTranslations");
                entity.HasKey(pt => pt.Id);
                entity.Property(pt => pt.ProductId).HasColumnName("product_id");
                entity.Property(pt => pt.LangCode).HasColumnName("lang_code");
                entity.Property(pt => pt.ProductName).HasColumnName("product_name");
                entity.Property(pt => pt.ProductDesc).HasColumnName("product_desc");
            });

            // 12. Seeding Data thay cho MockDataController
            // 12. Seeding Data
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    // 💡 Dán cứng chuỗi băm của "123456" vào đây (BCrypt băm sẵn)
                    PasswordHash = "$2a$11$n/A1qU55YyC7o2s1K0kC1O/0wA1oHh5X2w3E1z8e7H7A9R2lX4m",
                    FullName = "System Admin",
                    Role = "Admin"
                }
            );
        }
    }
}