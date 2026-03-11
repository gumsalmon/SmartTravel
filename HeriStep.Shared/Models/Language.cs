using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Languages")]
    public class Language
    {
        [Key] // lang_code là khóa chính
        public string LangCode { get; set; } = string.Empty;

        public string LangName { get; set; } = string.Empty;

        public string? FlagIconUrl { get; set; }
    }
}