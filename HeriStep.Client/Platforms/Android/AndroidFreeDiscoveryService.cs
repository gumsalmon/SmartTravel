using Android.Content;
using HeriStep.Client.Models.LocalModels;
using HeriStep.Client.Services;

namespace HeriStep.Client.Platforms.Android
{
    /// <summary>
    /// Android implementation của IFreeDiscoveryService.
    /// Start/Stop GpsDiscoveryForegroundService qua Intent.
    /// GeofenceEngine chạy BÊN TRONG service — không chạy trực tiếp ở đây.
    /// </summary>
    public class AndroidFreeDiscoveryService : IFreeDiscoveryService
    {
        private readonly GeofenceEngine _engine;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public event Action<LocalStall, double>? StallEntered;

        public AndroidFreeDiscoveryService(GeofenceEngine engine)
        {
            _engine = engine;

            // Forward engine event lên ViewModel
            _engine.StallEntered += (stall, dist) =>
                StallEntered?.Invoke(stall, dist);
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            // Bước 1: Start Android Foreground Service
            StartForegroundService();

            // Bước 2: Engine sẽ được Start() bên trong Service.
            // Tuy nhiên nếu cần control trực tiếp (debug mode), gọi ở đây.
            // await _engine.StartAsync();

            _isRunning = true;
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            StopForegroundService();

            // Engine.StopAsync() được gọi bên trong OnDestroy của Service
            _isRunning = false;

            await Task.CompletedTask;
        }

        public void ResetSession()
        {
            _engine.ResetVisitedFlags();
        }

        // ───────────────────────────────────────────────────────────────
        //  Android Intent helpers
        // ───────────────────────────────────────────────────────────────

        private static void StartForegroundService()
        {
            var context = global::Android.App.Application.Context;
            var intent  = new Intent(context, typeof(GpsDiscoveryForegroundService));
            intent.SetAction(GpsDiscoveryForegroundService.ActionStart);

            // Android 8+: phải dùng StartForegroundService
            if (global::Android.OS.Build.VERSION.SdkInt >=
                global::Android.OS.BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        private static void StopForegroundService()
        {
            var context = global::Android.App.Application.Context;
            var intent  = new Intent(context, typeof(GpsDiscoveryForegroundService));
            intent.SetAction(GpsDiscoveryForegroundService.ActionStop);
            context.StartService(intent);
        }
    }
}
