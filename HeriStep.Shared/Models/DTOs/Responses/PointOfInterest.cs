using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HeriStep.Shared.Models.DTOs.Responses
{
    /// <summary>
    /// Model đại diện cho thông tin Sạp hàng trên bản đồ.
    /// Mapping trực tiếp với bảng [Stalls] trong Database Enterprise.
    /// </summary>
    [Table("Stalls")]
    public class PointOfInterest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [Column("owner_id")]
        [JsonPropertyName("ownerId")]
        public int? OwnerId { get; set; }

        [Column("sort_order")]
        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; } = 0;

        [Column("TourID")]
        [JsonPropertyName("tourId")]
        public int? TourID { get; set; }

        [Required(ErrorMessage = "❌ Vui lòng nhập tên sạp!")]
        [Column("name_default")]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "❌ Thiếu tọa độ vĩ độ!")]
        [Range(-90.0, 90.0, ErrorMessage = "❌ Vĩ độ phải từ -90 đến 90")]
        [Column("latitude")]
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "❌ Thiếu tọa độ kinh độ!")]
        [Range(-180.0, 180.0, ErrorMessage = "❌ Kinh độ phải từ -180 đến 180")]
        [Column("longitude")]
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [Range(0, 500, ErrorMessage = "❌ Bán kính phải từ 0 đến 500m")]
        [Column("radius_meter")]
        [JsonPropertyName("radiusMeter")]
        public int RadiusMeter { get; set; } = 50;

        [Column("image_thumb")]
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [Column("is_open")]
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; } = true;

        [Column("is_deleted")]
        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("audio_url")]
        [JsonPropertyName("audioUrl")]
        public string? AudioUrl { get; set; }

        [Column("priority")]
        [JsonPropertyName("priority")]
        public int Priority { get; set; } = 0;

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }


        // ============================================================
        // CÁC THUỘC TÍNH ẢO (KHÔNG LƯU DB - DÙNG ĐỂ HIỂN THỊ TRÊN APP/WEB)
        // ============================================================

        [NotMapped]
        [JsonPropertyName("ttsScript")]
        public string TtsScript { get; set; } = "Chào mừng bạn đến với sạp hàng của chúng tôi!";

        [NotMapped]
        [JsonPropertyName("ownerName")]
        public string? OwnerName { get; set; }

        /// <summary>
        /// Tính toán xem sạp còn hạn hay đã hết hạn thanh toán (Dùng cho Admin Web)
        /// </summary>
        [NotMapped]
        [JsonPropertyName("isExpired")]
        public bool IsExpired { get; set; } = false;
    }
}
