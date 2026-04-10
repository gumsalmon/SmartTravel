using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Subscriptions")]
    public class Subscription
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("stall_id")] public int StallId { get; set; }
        [Column("device_id")] public string DeviceId { get; set; } = string.Empty;
        [Column("activation_code")] public string? ActivationCode { get; set; }
        [Column("start_date")] public DateTime? StartDate { get; set; } = DateTime.Now;
        [Column("expiry_date")] public DateTime? ExpiryDate { get; set; }
        [Column("is_active")] public bool IsActive { get; set; } = true;

        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? UpdatedAt { get; set; }

        // 👇 THÊM 2 DÒNG NÀY ĐỂ FIX LỖI NHÉ SẾP 👇
        [ForeignKey("StallId")]
        public virtual Stall? Stall { get; set; }
    }
}