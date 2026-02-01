using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Thư viện quan trọng để đồng bộ JSON

namespace HeriStep.Shared
{
    [Table("Stalls")] // Ánh xạ đúng tên bảng trong SQL Server
    public class PointOfInterest
    {
        [Key]
        [JsonPropertyName("id")] // Khớp với 'id' trong JSON
        public int Id { get; set; }

        [Column("name_default")]
        [JsonPropertyName("name")] // Khớp với 'name' trong JSON
        public string? Name { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [Column("radius_meter")]
        [JsonPropertyName("radius")]
        public int Radius { get; set; }

        [Column("image_thumb")]
        [JsonPropertyName("imageUrl")] // Khớp với 'imageUrl' trong JSON
        public string? ImageUrl { get; set; }

        [Column("is_open")]
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; }

        [Column("updated_at")]
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // TourId để liên kết với bảng Tours
        [JsonPropertyName("tourId")]
        public int? TourId { get; set; }
    }
}