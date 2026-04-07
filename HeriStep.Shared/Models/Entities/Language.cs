using System; // 💡 THÊM DÒNG NÀY ĐỂ DÙNG DATETIME
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Languages")]
    public class Language
    {
        [Key] // lang_code là khóa chính
        [Column("lang_code")]
        public string LangCode { get; set; } = string.Empty;

        [Column("lang_name")]
        public string LangName { get; set; } = string.Empty;

        [Column("flag_icon_url")]
        public string? FlagIconUrl { get; set; }

        // 💡 TECH LEAD ĐÃ THÊM:
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
