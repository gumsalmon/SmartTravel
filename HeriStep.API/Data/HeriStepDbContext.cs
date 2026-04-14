using HeriStep.Shared;
using HeriStep.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;

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
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductTranslation> ProductTranslations { get; set; }
        public DbSet<TicketPackage> TicketPackages { get; set; }
        public DbSet<TouristTicket> TouristTickets { get; set; }
        public DbSet<SubscriptionTransaction> SubscriptionTransactions { get; set; }
        public DbSet<TouristTrajectory> TouristTrajectories { get; set; }

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
                // Mapping Delta Sync & Soft Delete
                entity.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            });

            // 2. Cấu hình bảng Tours (Có Trigger)
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.ToTable("Tours", tb => tb.HasTrigger("TRG_UpdateTourTime"));
                entity.Property(t => t.TourName).HasColumnName("tour_name");
                entity.Property(t => t.ImageUrl).HasColumnName("image_url");
                entity.Property(t => t.Description).HasColumnName("description");
                entity.Property(t => t.IsActive).HasColumnName("is_active");
                entity.Property(t => t.IsTopHot).HasColumnName("is_top_hot");
                entity.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            });

            // 3. Cấu hình bảng Stalls (Có Trigger)
            modelBuilder.Entity<Stall>(entity =>
            {
                entity.ToTable("Stalls", tb => tb.HasTrigger("TRG_UpdateStallTime"));
                entity.Property(p => p.Name).HasColumnName("name_default");
                entity.Property(p => p.OwnerId).HasColumnName("owner_id");
                entity.Property(p => p.SortOrder).HasColumnName("sort_order");
                entity.Property(p => p.TourID).HasColumnName("TourID");
                entity.Property(p => p.RadiusMeter).HasColumnName("radius_meter");
                entity.Property(p => p.IsOpen).HasColumnName("is_open");
                entity.Property(p => p.ImageUrl).HasColumnName("image_thumb");
                entity.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

                entity.Ignore(p => p.TtsScript);
                entity.Ignore(p => p.OwnerName);
            });

            // 4. Cấu hình bảng StallContents (Có Trigger)
            modelBuilder.Entity<StallContent>(entity =>
            {
                entity.ToTable("StallContents", tb => tb.HasTrigger("TRG_UpdateContentTime"));
                entity.Property(c => c.StallId).HasColumnName("stall_id");
                entity.Property(c => c.LangCode).HasColumnName("lang_code");
                entity.Property(c => c.TtsScript).HasColumnName("tts_script");
                entity.Property(c => c.IsActive).HasColumnName("is_active");
                entity.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(c => c.UpdatedAt).HasColumnName("updated_at");
                entity.Property(c=> c.IsProcessed).HasColumnName("is_processed").HasDefaultValue(false);
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
                entity.Property(s => s.UpdatedAt).HasColumnName("updated_at");
            });

            // 6. Cấu hình bảng Languages
            modelBuilder.Entity<Language>(entity =>
            {
                entity.ToTable("Languages");
                entity.HasKey(l => l.LangCode);
                entity.Property(l => l.LangCode).HasColumnName("lang_code");
                entity.Property(l => l.LangName).HasColumnName("lang_name");
                entity.Property(l => l.FlagIconUrl).HasColumnName("flag_icon_url");
                entity.Property(l => l.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(l => l.UpdatedAt).HasColumnName("updated_at");
            });

            // 7. Cấu hình bảng Products (Có Trigger)
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products", tb => tb.HasTrigger("TRG_UpdateProductTime"));
                entity.Property(p => p.StallId).HasColumnName("stall_id");
                entity.Property(p => p.BasePrice).HasColumnType("decimal(18,2)").HasColumnName("base_price");
                entity.Property(p => p.ImageUrl).HasColumnName("image_url");
                entity.Property(p => p.IsSignature).HasColumnName("is_signature");
                entity.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            });

            // 8. Cấu hình bảng StallVisits (💡 Đã đổi khóa chính sang GUID cho Offline Sync)
            modelBuilder.Entity<StallVisit>(entity =>
            {
                entity.ToTable("StallVisits");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Id).ValueGeneratedOnAdd(); // Cho phép tự sinh UUID
                entity.Property(v => v.StallId).HasColumnName("stall_id");
                entity.Property(v => v.DeviceId).HasColumnName("device_id");
                entity.Property(v => v.VisitedAt).HasColumnName("visited_at");
                entity.Property(v => v.CreatedAtServer).HasColumnName("created_at_server");
                entity.Property(v => v.ListenDurationSeconds).HasColumnName("listen_duration_seconds");
            });

            // 9. Cấu hình bảng TouristTrajectories
            modelBuilder.Entity<TouristTrajectory>(entity =>
            {
                entity.ToTable("TouristTrajectories");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.DeviceId).HasColumnName("device_id");
                entity.Property(t => t.Latitude).HasColumnName("latitude");
                entity.Property(t => t.Longitude).HasColumnName("longitude");
                entity.Property(t => t.RecordedAt).HasColumnName("recorded_at");
            });

            // 10. Cấu hình bảng TicketPackages
            modelBuilder.Entity<TicketPackage>(entity =>
            {
                entity.ToTable("TicketPackages");
                entity.HasKey(t => t.Id);
                entity.Property(t => t.PackageName).HasColumnName("package_name");
                entity.Property(t => t.Price).HasColumnType("decimal(18,2)").HasColumnName("price");
                entity.Property(t => t.DurationHours).HasColumnName("duration_hours");
                entity.Property(t => t.IsActive).HasColumnName("is_active");
                entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            });

            // 11. Cấu hình bảng TouristTickets
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
                entity.Ignore(t => t.PackageName);
            });

            // 12. Cấu hình bảng ProductTranslations (Có Trigger)
            modelBuilder.Entity<ProductTranslation>(entity =>
            {
                entity.ToTable("ProductTranslations", tb => tb.HasTrigger("TRG_UpdateTranslationTime"));
                entity.HasKey(pt => pt.Id);
                entity.Property(pt => pt.ProductId).HasColumnName("product_id");
                entity.Property(pt => pt.LangCode).HasColumnName("lang_code");
                entity.Property(pt => pt.ProductName).HasColumnName("product_name");
                entity.Property(pt => pt.ProductDesc).HasColumnName("product_desc");
                entity.Property(pt => pt.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
                entity.Property(pt => pt.UpdatedAt).HasColumnName("updated_at");
                entity.Property(c => c.IsProcessed).HasColumnName("is_processed").HasDefaultValue(false);

            });

            // 13. Seeding User Admin mặc định
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", PasswordHash = "$2a$11$n/A1qU55YyC7o2s1K0kC1O/0wA1oHh5X2w3E1z8e7H7A9R2lX4m", FullName = "System Admin", Role = "Admin", IsDeleted = false }
            );
        }
    }
}
