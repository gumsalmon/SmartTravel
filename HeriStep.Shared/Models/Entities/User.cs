using System; // 💡 THÊM DÒNG NÀY ĐỂ DÙNG DATETIME
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("role")]
        public string Role { get; set; } = "StallOwner";

        // 💡 TECH LEAD ĐÃ THÊM:
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
