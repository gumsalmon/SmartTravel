using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using HeriStep.Client.Services;
using MauiApplication = Microsoft.Maui.Controls.Application;

namespace HeriStep.Client.Platforms.Android
{
    /// <summary>
    /// ─────────────────────────────────────────────────────────────────
    /// GPS DISCOVERY FOREGROUND SERVICE
    /// ─────────────────────────────────────────────────────────────────
    /// Mục đích: Giữ process Android sống khi màn hình tắt để GeofenceEngine
    /// tiếp tục vòng lặp GPS 5 giây → TTS phát ngay kể cả khi điện thoại
    /// đang ở trong túi khách.
    ///
    /// Khai báo bắt buộc trong AndroidManifest.xml:
    ///   <service android:name=".GpsDiscoveryForegroundService"
    ///            android:foregroundServiceType="location"
    ///            android:exported="false" />
    /// ─────────────────────────────────────────────────────────────────
    /// </summary>
    [Service(
        Name = "com.sgu.myapp.GpsDiscoveryForegroundService",
        ForegroundServiceType = ForegroundService.TypeLocation,
        Exported = false)]
    public class GpsDiscoveryForegroundService : Service
    {
        // ── Notification channel ────────────────────────────────────────
        private const string ChannelId   = "heristep_gps_channel";
        private const string ChannelName = "HeriStep GPS Discovery";
        private const int    NotifId     = 9_001;

        // ── Intent actions (dùng để Start/Stop từ MAUI layer) ──────────
        public const string ActionStart = "com.sgu.myapp.GPS_START";
        public const string ActionStop  = "com.sgu.myapp.GPS_STOP";

        // ── Engine reference (singleton từ DI) ─────────────────────────
        private GeofenceEngine? _engine;
        private CancellationTokenSource? _engineCts;

        // ═══════════════════════════════════════════════════════════════
        //  SERVICE LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        public override IBinder? OnBind(Intent? intent) => null; // Unbound service

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Interoperability", "CA1416",
            Justification = "Guarded by API level checks")]
        public override StartCommandResult OnStartCommand(
            Intent? intent, StartCommandFlags flags, int startId)
        {
            switch (intent?.Action)
            {
                case ActionStart:
                    StartForegroundWithNotification();
                    StartEngineAsync();
                    break;

                case ActionStop:
                    StopEngineAsync();
                    StopForeground(StopForegroundFlags.Remove);
                    StopSelf();
                    break;
            }

            // STICKY: Android sẽ restart service tự động nếu bị kill
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            StopEngineAsync();
            base.OnDestroy();
        }

        // ═══════════════════════════════════════════════════════════════
        //  FOREGROUND NOTIFICATION
        // ═══════════════════════════════════════════════════════════════

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Interoperability", "CA1416",
            Justification = "Method only called on Android ≥ API 21")]
        private void StartForegroundWithNotification()
        {
            CreateNotificationChannel();

            var notification = BuildNotification();

            // Android 14+ yêu cầu chỉ định foregroundServiceType tường minh
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(NotifId, notification,
                    global::Android.Content.PM.ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NotifId, notification);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416")]
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var notificationManager =
                (NotificationManager?)GetSystemService(NotificationService);

            if (notificationManager?.GetNotificationChannel(ChannelId) is not null)
                return; // đã tồn tại

            var channel = new NotificationChannel(
                ChannelId,
                ChannelName,
                NotificationImportance.Low) // Low = không phát âm thanh chuông
            {
                Description = "Kênh thông báo cho chế độ Khám Phá Tự Do GPS"
            };

            notificationManager?.CreateNotificationChannel(channel);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416")]
        private Notification BuildNotification()
        {
            // Intent để mở lại App khi tap notification
            var openAppIntent = new Intent(this, typeof(MainActivity));
            openAppIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            var pendingFlag = Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;
            var pendingIntent = PendingIntent.GetActivity(this, 0, openAppIntent, pendingFlag);

            // Intent để Stop service từ notification action
            var stopIntent = new Intent(this, typeof(GpsDiscoveryForegroundService));
            stopIntent.SetAction(ActionStop);
            var stopPending = PendingIntent.GetService(this, 1, stopIntent, pendingFlag);

            return new Notification.Builder(this, ChannelId)
                .SetContentTitle("🗺️ Chế độ Khám Phá Tự Do")
                .SetContentText("Đang quét GPS — đi dạo và khám phá các sạp xung quanh!")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)           // không thể vuốt bỏ
                .AddAction(
                    new Notification.Action.Builder(
                        null, "Dừng lại", stopPending).Build())
                .Build();
        }

        // ═══════════════════════════════════════════════════════════════
        //  ENGINE — lấy từ MAUI DI Container
        // ═══════════════════════════════════════════════════════════════

        private void StartEngineAsync()
        {
            _engineCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    // Lấy engine từ MAUI's DI (singleton)
                    _engine = IPlatformApplication.Current?.Services
                                  .GetService<GeofenceEngine>();

                    if (_engine is null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "[ForegroundSvc] ⚠️ GeofenceEngine not registered in DI!");
                        return;
                    }

                    await _engine.StartAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[ForegroundSvc] Engine start error: {ex.Message}");
                }
            }, _engineCts.Token);
        }

        private void StopEngineAsync()
        {
            _engineCts?.Cancel();
            _engineCts?.Dispose();
            _engineCts = null;

            // Gọi StopAsync nhưng không await (OnDestroy không phải async)
            _engine?.StopAsync()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            System.Diagnostics.Debug.WriteLine(
                                $"[ForegroundSvc] Engine stop error: {t.Exception?.Message}");
                    });

            _engine = null;
        }
    }
}
