using System; // 💡 THÊM DÒNG NÀY ĐỂ DÙNG DATETIME
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("TicketPackages")]
    public class TicketPackage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("package_name")]
        public string? PackageName { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("duration_hours")]
        public int DurationHours { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        // 💡 TECH LEAD ĐÃ THÊM:
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
