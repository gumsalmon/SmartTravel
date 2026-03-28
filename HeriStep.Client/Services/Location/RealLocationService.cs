#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;

namespace HeriStep.Client.Services.Location
{
    public class RealLocationService : ILocationService
    {
        // 💡 Dùng đường dẫn đầy đủ ở kiểu trả về
        public async Task<Microsoft.Maui.Devices.Sensors.Location?> GetLocationAsync()
        {
            try
            {
#pragma warning disable CA1416 // 💡 Tắt cảnh báo "Chỉ hỗ trợ Windows 10.xxx"
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);
                return location;
#pragma warning restore CA1416 // 💡 Bật lại cảnh báo
            }
            catch (Exception ex)
            {
                // Bắt lỗi khi không có quyền GPS hoặc tắt GPS
                System.Diagnostics.Debug.WriteLine($"Lỗi lấy GPS thực: {ex.Message}");
                return null;
            }
        }
    }
}