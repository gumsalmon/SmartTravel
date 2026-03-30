using HeriStep.Shared.Models;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// GeofenceService - Dịch vụ Radar Không Gian
    /// Dùng thuật toán Haversine tính khoảng cách Kinh/Vĩ độ thực tế giữa 2 điểm trên Trái Đất
    /// Khi khách giẫm vào vùng bán kính của Sạp, tự động bắn sự kiện → TTS phát thanh
    /// </summary>
    public class GeofenceService
    {
        private const double EarthRadiusKm = 6371.0;
        private readonly HashSet<int> _announcedStalls = new(); // Chống spam loa liên tục

        public event Action<Stall>? StallEntered; // Sự kiện bắn ra khi khách vào gần Sạp

        /// <summary>
        /// Kiểm tra vị trí hiện tại với toàn bộ danh sách Sạp
        /// Gọi hàm này mỗi 5 giây từ background loop
        /// </summary>
        public void CheckProximity(double userLat, double userLon, IEnumerable<Stall> stalls)
        {
            foreach (var stall in stalls)
            {
                if (stall.Latitude == 0 && stall.Longitude == 0) continue;

                double distanceMeters = HaversineDistanceMeters(userLat, userLon, stall.Latitude, stall.Longitude);

                double radiusM = stall.RadiusMeter > 0 ? stall.RadiusMeter : 50.0;

                if (distanceMeters <= radiusM)
                {
                    // Chưa thông báo lần nào → bắn sự kiện
                    if (_announcedStalls.Add(stall.Id))
                    {
                        System.Diagnostics.Debug.WriteLine($"[GEOFENCE] Vào vùng Sạp '{stall.Name}' - Khoảng cách: {distanceMeters:F1}m");
                        StallEntered?.Invoke(stall);
                    }
                }
                else
                {
                    // Ra khỏi vùng → xóa khỏi danh sách đã thông báo để lần sau vào lại được thông báo tiếp
                    _announcedStalls.Remove(stall.Id);
                }
            }
        }

        /// <summary>
        /// Thuật toán Haversine: Tính khoảng cách (mét) giữa 2 tọa độ GPS trên hình cầu Trái Đất
        /// </summary>
        private static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c * 1000; // Đổi km → mét
        }

        private static double ToRad(double deg) => deg * Math.PI / 180;
    }
}
