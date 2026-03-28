#nullable enable
using System.Threading.Tasks;

namespace HeriStep.Client.Services.Location
{
    public interface ILocationService
    {
        // 💡 Dùng đường dẫn đầy đủ để tránh đụng hàng với namespace "Location" của sếp
        Task<Microsoft.Maui.Devices.Sensors.Location?> GetLocationAsync();
    }
}