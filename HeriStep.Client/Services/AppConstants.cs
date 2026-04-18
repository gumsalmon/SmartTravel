namespace HeriStep.Client.Services
{
    public static class AppConstants
    {
        // ── Backend API (port 5297)
        public static string BaseApiUrl =
            Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android
            ? "http://10.0.2.2:5297"
            : "http://localhost:5297";

        // ── Admin Web SignalR Hub (port 5287)
        // Emulator Android → 10.0.2.2 thay thế localhost của máy host
        // Thiết bị thật cùng WiFi → dùng IP LAN của máy host (VD: 192.168.1.x)
        public static string AdminHubUrl =
            Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android
            ? "http://10.0.2.2:5287/dashboardHub"
            : "http://localhost:5287/dashboardHub";
    }
}
