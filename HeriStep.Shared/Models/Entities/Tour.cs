using System.Collections.Generic;
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

        // 2. 💡 ĐÃ SỬA: Xóa [NotMapped] và đổi thành virtual ICollection
        // Để EF Core biết đây là Navigation Property (Mối quan hệ 1 Tour - Nhiều Sạp)
        // và cho phép lệnh .Include(t => t.Stalls) hoạt động!
        public virtual ICollection<Stall> Stalls { get; set; } = new List<Stall>();

        // 3. Phân loại loại lộ trình dựa trên số lượng quán
        [NotMapped]
        public string DurationType => StallCount < 4 ? "Ngắn" : "Dài";
    }
}