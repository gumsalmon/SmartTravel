using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    // Class này đại diện cho bảng 'Stalls' dưới SQL
    [Table("Stalls")]
    public class Stall
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("owner_id")]
        public int? OwnerId { get; set; }

        [Column("TourID")]
        public int? TourID { get; set; }

        [Column("name_default")] // Ánh xạ với cột name_default trong SQL
        public string Name { get; set; } = string.Empty;

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("radius_meter")]
        public int RadiusMeter { get; set; } = 50;

        [Column("is_open")]
        public bool IsOpen { get; set; } = true;

        [Column("image_thumb")]
        public string? ImageUrl { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // ==========================================================
        // KHU VỰC ĐÃ PHONG ẤN BẰNG [NotMapped] (CHỈ XÀI TRÊN WEB/APP)
        // ==========================================================

        [NotMapped] // 💡 ĐÃ FIX: Database không có cột này
        public string? Category { get; set; }

        [NotMapped] // 💡 ĐÃ FIX: Hủy cái [Column("subscription_level")] đi, vì DB không có
        public int Level { get; set; }

        [NotMapped] // Chỉ dùng hiển thị UI, DB không có cột này
        public string? OwnerName { get; set; }

        [NotMapped]
        public double? Radius { get; set; }

        [NotMapped] // Cái này nằm bên bảng StallContents, không nằm ở đây!
        public string? TtsScript { get; set; }
    }
}