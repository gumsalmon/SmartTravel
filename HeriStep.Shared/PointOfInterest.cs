using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HeriStep.Shared
{
    [Table("Stalls")] // Ánh xạ đúng tên bảng trong SQL Server
    public class PointOfInterest
    {
        [Key]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        // BẮT BUỘC NHẬP CHỦ SẠP: Nếu để trống, web sẽ báo lỗi ngay lập tức
        [Required(ErrorMessage = "❌ Vui lòng chọn tài khoản Chủ sạp!")]
        [Column("owner_id")]
        [JsonPropertyName("ownerId")]
        public int? OwnerId { get; set; }

        // BẮT BUỘC NHẬP TÊN SẠP
        [Required(ErrorMessage = "❌ Vui lòng nhập tên sạp!")]
        [Column("name_default")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // CHẶN LỖI SQL: Giới hạn Vĩ độ từ -90 đến 90
        [Range(-90.0, 90.0, ErrorMessage = "❌ Vĩ độ (Latitude) phải nằm trong khoảng -90 đến 90")]
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        // CHẶN LỖI SQL: Giới hạn Kinh độ từ -180 đến 180
        [Range(-180.0, 180.0, ErrorMessage = "❌ Kinh độ (Longitude) phải nằm trong khoảng -180 đến 180")]
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [Column("radius_meter")]
        [JsonPropertyName("radiusMeter")]
        public int RadiusMeter { get; set; } = 50;

        [Column("image_thumb")]
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [Column("is_open")]
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; } = true;

        [Column("updated_at")]
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("TourID")]
        [JsonPropertyName("tourId")]
        public int? TourID { get; set; }

        [NotMapped]
        [JsonPropertyName("ttsScript")]
        public string TtsScript { get; set; } = string.Empty;
    }
}