using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared
{
    [Table("StallContents")] // Khớp với tên bảng trong ảnh SSMS của bạn
    public class StallContent
    {
        [Key]
        public int id { get; set; }

        [Column("stall_id")]
        public int StallId { get; set; } // Phải là 'int' để khớp với bảng Stalls

        [Column("lang_code")]
        public string LangCode { get; set; } = "vi";

        [Column("tts_script")]
        public string TtsScript { get; set; } = string.Empty;

        [Column("is_active")]
        public bool? IsActive { get; set; }
    }
}