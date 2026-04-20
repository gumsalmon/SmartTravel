using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HeriStep.Shared.Models
{
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

        [Column("name_default")]
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

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("audio_url")]
        public string? AudioUrl { get; set; }

        [Column("priority")]
        public int Priority { get; set; } = 0;

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? UpdatedAt { get; set; }

        // --- CÁC TRƯỜNG KHÔNG LƯU SQL NHƯNG CẦN CHO WEB/APP ---
        [NotMapped] public string? Category { get; set; }
        [NotMapped] public int Level { get; set; }
        [NotMapped] public string? OwnerName { get; set; }
        [NotMapped] public double? Radius { get; set; }
        [NotMapped] public bool IsExpired { get; set; }
        [NotMapped] public string? Description { get; set; }
        [NotMapped] public string? TtsScript { get; set; } // 💡 CÁI SẾP ĐANG THIẾU ĐÂY!

        // 💡 NGĂN VÒNG LẶP JSON
        [JsonIgnore]
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}