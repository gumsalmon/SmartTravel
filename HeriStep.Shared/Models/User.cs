using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Column("username")] // Khớp với SQL
    public string Username { get; set; } = string.Empty;

    [Column("password_hash")] // Khớp với SQL
    public string PasswordHash { get; set; } = string.Empty;

    [Column("full_name")] // Khớp với SQL - ĐÂY LÀ CHỖ GÂY LỖI LÚC NÃY
    public string? FullName { get; set; }

    [Column("role")]
    public string Role { get; set; } = "StallOwner";
}