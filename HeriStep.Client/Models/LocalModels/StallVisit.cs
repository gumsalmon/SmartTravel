using SQLite;

namespace HeriStep.Client.Models.LocalModels
{
    /// <summary>
    /// Bảng ghi log lượt ghé thăm cho tính năng Heatmap.
    /// MỖI LẦN khách đi ngang một sạp → INSERT 1 dòng mới.
    /// Không bao giờ UPDATE bảng StallCache để bảo vệ dữ liệu gốc.
    ///
    /// Query Heatmap: SELECT StallId, COUNT(*) as VisitCount
    ///                FROM StallVisits
    ///                GROUP BY StallId
    ///                ORDER BY VisitCount DESC
    /// </summary>
    [Table("StallVisits")]
    public class StallVisit
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>FK → StallCache.Id</summary>
        [Indexed]
        public int StallId { get; set; }

        /// <summary>Tên sạp tại thời điểm ghé thăm (snapshot, tránh JOIN)</summary>
        public string StallName { get; set; } = string.Empty;

        /// <summary>Tọa độ thực tế của thiết bị khi trigger TTS (có thể lệch ≤20m so với sạp)</summary>
        public double DeviceLat { get; set; }
        public double DeviceLng { get; set; }

        /// <summary>
        /// Khoảng cách thực tế (mét) tại thời điểm trigger.
        /// Dùng để phân tích: khách thường đứng cách bao xa mới nghe TTS?
        /// </summary>
        public double DistanceMeters { get; set; }

        /// <summary>UTC timestamp. Dùng GROUP BY strftime('%H', VisitedAt) để phân tích giờ cao điểm.</summary>
        public DateTime VisitedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Phiên làm việc (mỗi lần bật Free Discovery = 1 SessionId mới)</summary>
        public string SessionId { get; set; } = string.Empty;
    }
}
