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

        [Column("name_default")]
        [JsonPropertyName("name")]
        // Khởi tạo string.Empty để hết cảnh báo Non-nullable
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [Column("radius_meter")]
        [JsonPropertyName("radius")]
        public int Radius { get; set; }

        [Column("image_thumb")]
        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [Column("is_open")]
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; }

        [Column("updated_at")]
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("TourID")] // Khớp với lệnh Rename trong SQL của bạn
        [JsonPropertyName("tourId")]
        public int? TourID { get; set; }

        // Sửa lỗi 'Invalid column name TtsScript'
        // Nếu trong SQL cột này tên là tts_script, hãy khai báo ở đây
        [Column("tts_script")]
        [JsonPropertyName("ttsScript")]
        public string TtsScript { get; set; } = string.Empty;

        // XÓA DÒNG DbSet<User> TẠI ĐÂY - NÓ PHẢI Ở TRONG DbContext
    }
}