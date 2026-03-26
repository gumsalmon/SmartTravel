#nullable enable
using System;
using System.Threading.Tasks;

namespace HeriStep.Client.Services.Location
{
    public class MockLocationService : ILocationService
    {
        private double _baseLat = 10.7630;
        private double _baseLon = 106.6600;
        private int _step = 0;

        // 💡 Dùng đường dẫn đầy đủ ở kiểu trả về
        public Task<Microsoft.Maui.Devices.Sensors.Location?> GetLocationAsync()
        {
            // Tịnh tiến tọa độ mỗi lần gọi để giả lập người đang đi bộ
            _step++;
            double currentLat = _baseLat + (_step * 0.0001);
            double currentLon = _baseLon + (_step * 0.0001);

            return Task.FromResult<Microsoft.Maui.Devices.Sensors.Location?>(
                new Microsoft.Maui.Devices.Sensors.Location(currentLat, currentLon));
        }
    }
}