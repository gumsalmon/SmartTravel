using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        [Column("id")] // Ép kiểu về đúng tên cột trong SQL
        public int Id { get; set; }

        [Column("stall_id")]
        public int StallId { get; set; }

        [Column("base_price")]
        public decimal BasePrice { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("is_signature")]
        public bool IsSignature { get; set; } = false;

        // 💡 QUAN TRỌNG: Biến ảo để hứng tên món ăn từ bảng ProductTranslations (Không lưu vào DB)
        [NotMapped]
        public string? ProductName { get; set; }
    }
}