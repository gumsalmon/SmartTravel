namespace HeriStep.Client.Services
{
    /// <summary>
    /// Abstraction layer để bridge MAUI cross-platform code ↔ Android Foreground Service.
    /// Trên Android → Start/Stop GpsDiscoveryForegroundService.
    /// Trên iOS      → GeofenceEngine chạy trực tiếp (không cần Foreground Service).
    /// </summary>
    public interface IFreeDiscoveryService
    {
        bool IsRunning { get; }
        Task StartAsync();
        Task StopAsync();
        void ResetSession();

        // Cho ViewModel subscribe để cập nhật UI
        event Action<Models.LocalModels.LocalStall, double>? StallEntered;
    }
}
