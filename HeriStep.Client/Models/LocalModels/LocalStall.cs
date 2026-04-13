using SQLite;

namespace HeriStep.Client.Models.LocalModels
{
    [Table("StallCache")]
    public class LocalStall
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }
        
        public string ImageUrl { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public bool IsOpen { get; set; }
        public bool HasOwner { get; set; }
        
        public double RadiusMeter { get; set; }
        
        // Điểm đánh giá để sắp xếp Top 5 (ORDER BY Rating DESC)
        public double Rating { get; set; } = 4.5;
        
        // Số lượt xem/ghé thăm
        public int VisitCount { get; set; } = 0;

        /// <summary>
        /// Kịch bản âm thanh TTS cho chế độ Khám Phá Tự Do.
        /// VD: "Chào mừng bạn đến với sạp Bánh Mì Hội An! Hôm nay chúng tôi có..."
        /// Nếu để trống → GeofenceEngine sẽ bỏ qua, không phát âm thanh.
        /// </summary>
        [MaxLength(1000)]
        public string TtsScript { get; set; } = string.Empty;

        /// <summary>
        /// Chế độ Khám Phá Tự Do: Đánh dấu quán đã được phát TTS một lần.
        /// IsVisited = true → sẽ không đọc lại khi khách đứng yên trong khu vực.
        /// Trường này KHÔNG LƯU vào DB — chỉ tồn tại trên RAM trong session.
        /// Reset về false khi khởi động lại chế độ Khám Phá Tự Do.
        /// </summary>
        [SQLite.Ignore]
        public bool IsVisited { get; set; } = false;
    }
}
