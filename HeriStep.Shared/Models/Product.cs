using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public int StallId { get; set; }

        public decimal BasePrice { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsSignature { get; set; } = false;

        // 💡 QUAN TRỌNG: Thêm dòng này để khớp với cấu hình DbContext và hết lỗi ProductName
        [NotMapped]
        public string? ProductName { get; set; }
    }
}