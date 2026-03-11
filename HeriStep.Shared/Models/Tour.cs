using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    [Table("Tours")] // Ánh xạ đúng tên bảng trong SQL
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "❌ Tên Tour không được để trống")]
        // tour_name trong SQL là NOT NULL nên giữ nguyên string
        public string TourName { get; set; } = string.Empty;

        // 💡 GIẢI PHÁP: Thêm dấu ? để C# chấp nhận giá trị NULL từ SQL
        public string? Description { get; set; } = string.Empty;

        // 💡 GIẢI PHÁP: Thêm dấu ? để xử lý các tour chưa có ảnh
        public string? ImageUrl { get; set; } = "default-tour.jpg";

        // IsActive đã là bool? là rất chuẩn để tránh lỗi Data is Null
        public bool? IsActive { get; set; } = true;

        // Thuộc tính phụ để đếm số sạp hàng trong Tour (không lưu xuống DB)
        [NotMapped]
        public int StallCount { get; set; }
    }
}