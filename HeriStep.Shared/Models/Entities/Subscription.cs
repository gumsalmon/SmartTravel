using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HeriStep.Shared.Models
{
    [Table("Subscriptions")]
    public class Subscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("stall_id")]
        public int StallId { get; set; }

        [Required]
        [Column("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Column("activation_code")]
        public string? ActivationCode { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; } = true;

        // 💡 TECH LEAD ĐÃ THÊM:
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // BƯỚC QUAN TRỌNG: Ngăn chặn vòng lặp JSON bằng [JsonIgnore]
        // EF Core vẫn hiểu mối quan hệ (Include vẫn hoạt động), nhưng khi trả JSON về thì sẽ ẩn thông tin này đi.
        [JsonIgnore]
        public virtual Stall? Stall { get; set; }
    }
}
