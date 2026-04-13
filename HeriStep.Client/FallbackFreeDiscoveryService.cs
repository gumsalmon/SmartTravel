using HeriStep.Client.Models.LocalModels;
using HeriStep.Client.Services;

namespace HeriStep.Client
{
    /// <summary>
    /// Fallback cho Windows / MacCatalyst (dev/testing).
    /// GeofenceEngine chạy trực tiếp — không qua Foreground Service.
    /// </summary>
    public class FallbackFreeDiscoveryService : IFreeDiscoveryService
    {
        private readonly GeofenceEngine _engine;

        public bool IsRunning => _engine.IsRunning;

        public event Action<LocalStall, double>? StallEntered;

        public FallbackFreeDiscoveryService(GeofenceEngine engine)
        {
            _engine = engine;
            _engine.StallEntered += (s, d) => StallEntered?.Invoke(s, d);
        }

        public Task StartAsync()   => _engine.StartAsync();
        public Task StopAsync()    => _engine.StopAsync();
        public void ResetSession() => _engine.ResetVisitedFlags();
    }
}
