using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models // Nhớ kiểm tra đúng namespace của bạn
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("id")] // 💡 Thêm cái này để khớp hoàn toàn với SQL
        public int Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("role")]
        public string Role { get; set; } = "StallOwner";
    }
}