namespace HeriStep.Client.Services
{
    public static class AppConstants
    {
        // 10.0.2.2 is the special alias for your host loopback interface (127.0.0.1 on your development machine) 
        // when running on the Android emulator.
        public static string BaseApiUrl = "http://10.0.2.2:5297";
        
        // For physical devices, replace with your machine's IP address:
        // public static string BaseApiUrl = "http://192.168.1.x:5297";
    }
}
