namespace HeriStep.Client.Services
{
    public static class AppConstants
    {
        public static string BaseApiUrl = 
            Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android 
            ? "http://10.0.2.2:5297" 
            : "http://localhost:5297";
    }
}
