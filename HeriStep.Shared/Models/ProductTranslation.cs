using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("ProductTranslations")]
    public class ProductTranslation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("lang_code")]
        public string LangCode { get; set; } = "";

        [Column("product_name")]
        public string ProductName { get; set; } = "";

        [Column("product_desc")]
        public string? ProductDesc { get; set; }
    }
}