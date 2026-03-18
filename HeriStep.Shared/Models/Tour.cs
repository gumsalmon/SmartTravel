using System.ComponentModel.DataAnnotations.Schema;

namespace HeriStep.Shared.Models
{
    public class Tour
    {
        public int Id { get; set; }

        public string TourName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public bool? IsActive { get; set; } = true;

        public bool IsTopHot { get; set; } = false;

        // --- CÁC THUỘC TÍNH BỔ SUNG ĐỂ HIỂN THỊ (KHÔNG LƯU DB) ---

        // 1. Đếm số lượng sạp để phân loại Ngắn/Dài
        [NotMapped]
        public int StallCount { get; set; }

        // 2. 💡 QUAN TRỌNG: Danh sách chi tiết các quán trong lộ trình
        // Dùng để hiển thị ở trang Details.cshtml
        [NotMapped]
        public List<Stall> Stalls { get; set; } = new();

        // 3. Phân loại loại lộ trình dựa trên số lượng quán
        [NotMapped]
        public string DurationType => StallCount < 4 ? "Ngắn" : "Dài";
    }
}