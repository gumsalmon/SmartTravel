using System.ComponentModel.DataAnnotations;

namespace HeriStep.Shared.Models.DTOs.Responses
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "❌ Vui lòng nhập số điện thoại!")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "❌ Số điện thoại phải có 10 số và bắt đầu bằng 0")]
        [Display(Name = "Số điện thoại")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "❌ Vui lòng nhập tên chủ sạp!")]
        [StringLength(100, ErrorMessage = "❌ Tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Mật khẩu")]
        public string? Password { get; set; } // Dùng khi tạo mới hoặc Reset

        public string Role { get; set; } = "StallOwner";

        // --- THUỘC TÍNH MỚI BỔ SUNG ---
        // Dùng để hiển thị số lượng sạp ở trang danh sách mà không cần gọi API nhiều lần
        public int StallCount { get; set; }
    }
}