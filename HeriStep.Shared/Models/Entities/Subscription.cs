using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Subscriptions")]
    public class Subscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        // 💡 BẮT BUỘC PHẢI CÓ DÒNG NÀY ĐỂ MÓC VÀO BẢNG SẠP (STALLS)
        [Required]
        [Column("stall_id")]
        public int StallId { get; set; }

        [Required]
        [Column("device_id")] // Khớp với SQL: device_id
        public string DeviceId { get; set; } = string.Empty;

        [Column("activation_code")] // Khớp với SQL: activation_code
        public string? ActivationCode { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; } // Thêm ? để sửa lỗi Operator '?'

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; } // Thêm ? ở đây nữa

        [Column("is_active")]
        public bool? IsActive { get; set; } = true;
    }
}