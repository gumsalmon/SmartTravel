using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("TicketPackages")]
    public class TicketPackage
    {
        [Key]
        public int Id { get; set; }
        [Column("package_name")]
        public string? PackageName { get; set; }
        [Column("price")]
        public decimal Price { get; set; }
        [Column("duration_hours")]
        public int DurationHours { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}