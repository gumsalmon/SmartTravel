using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HeriStep.Shared.Models
{
    [Table("Stalls")]
    public class PointOfInterest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [Column("owner_id")]
        [JsonPropertyName("ownerId")]
        public int? OwnerId { get; set; }

        [Column("TourID")]
        [JsonPropertyName("tourId")]
        public int? TourID { get; set; }

        [Required(ErrorMessage = "❌ Vui lòng nhập tên sạp!")]
        [Column("name_default")]
        [JsonPropertyName("name")]
        public string? Name { get; set; } // Thêm ? để linh hoạt hơn cho Model Binder

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

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; } // Đã để DateTime? là rất chuẩn

        [NotMapped]
        [JsonPropertyName("ttsScript")]
        public string TtsScript { get; set; } = "Chào mừng bạn đến với sạp hàng của chúng tôi!";
    }
}