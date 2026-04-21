using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStep.Client.Models.LocalModels;
using HeriStep.Client.Services;

namespace HeriStep.Client.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Chế độ Khám Phá Tự Do.
    /// Bind trực tiếp vào FreeDiscoveryPage.xaml.
    ///
    /// Key design decisions:
    ///   - Không biết gì về Android/iOS — chỉ nói chuyện với IFreeDiscoveryService
    ///   - Mọi cập nhật UI đều qua MainThread.BeginInvokeOnMainThread
    ///   - IsRunning/NearbyStall dùng [ObservableProperty] của CommunityToolkit.Mvvm
    /// </summary>
    public partial class FreeDiscoveryViewModel : ObservableObject, IAsyncDisposable
    {
        private readonly IFreeDiscoveryService _discoveryService;

        // ── Observable state ────────────────────────────────────────────
        [ObservableProperty] private bool   _isRunning;
        [ObservableProperty] private string _statusText    = "Sẵn sàng khám phá!";
        [ObservableProperty] private string _stallNameText = string.Empty;
        [ObservableProperty] private bool   _showStallCard;
        [ObservableProperty] private int    _visitedCount;

        // Auto-hide timer cho StallCard
        private CancellationTokenSource? _cardHideCts;

        public FreeDiscoveryViewModel(IFreeDiscoveryService discoveryService)
        {
            _discoveryService = discoveryService;

            // Subscribe sự kiện từ Engine (callback trên background thread)
            _discoveryService.StallEntered += OnStallEntered;
        }

        // ═══════════════════════════════════════════════════════════════
        //  COMMANDS
        // ═══════════════════════════════════════════════════════════════

        [RelayCommand]
        private async Task ToggleDiscoveryAsync()
        {
            if (_discoveryService.IsRunning)
                await StopDiscoveryAsync();
            else
                await StartDiscoveryAsync();
        }

        [RelayCommand]
        private async Task StartDiscoveryAsync()
        {
            // Xin quyền Location nếu chưa có
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                StatusText = "⚠️ Cần quyền GPS để sử dụng tính năng này.";
                return;
            }

            // Xin quyền Notification cho Android 13+
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                var notifStatus = await Permissions.RequestAsync<Permissions.PostNotifications>();
                System.Diagnostics.Debug.WriteLine(
                    $"[ViewModel] Notification permission: {notifStatus}");
            }
#endif

            StatusText  = "🔍 Đang quét GPS... Hãy đi dạo!";
            IsRunning   = true;
            VisitedCount = 0;

            try
            {
                await _discoveryService.StartAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"❌ Lỗi khởi động: {ex.Message}";
                IsRunning  = false;
            }
        }

        [RelayCommand]
        private async Task StopDiscoveryAsync()
        {
            IsRunning  = false;
            StatusText = $"✅ Đã dừng. Bạn đã ghé thăm {VisitedCount} sạp hàng.";
            ShowStallCard = false;

            try
            {
                await _discoveryService.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Stop error: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ResetSession()
        {
            _discoveryService.ResetSession();
            VisitedCount  = 0;
            StatusText    = "🔄 Đã reset — bạn có thể nghe lại tất cả các sạp!";
            ShowStallCard = false;
        }

        // ═══════════════════════════════════════════════════════════════
        //  EVENT HANDLER — từ GeofenceEngine (BACKGROUND THREAD)
        // ═══════════════════════════════════════════════════════════════

        private void OnStallEntered(LocalStall stall, double distMeters)
        {
            // Phải marshal về Main Thread để cập nhật UI
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                VisitedCount++;
                StallNameText = $"🏪 {stall.Name}  ({distMeters:F0}m)";
                StatusText    = $"🔊 Đang phát giới thiệu sạp...";
                ShowStallCard = true;

                // Auto-hide StallCard sau 8 giây
                _cardHideCts?.Cancel();
                _cardHideCts = new CancellationTokenSource();
                try
                {
                    await Task.Delay(8_000, _cardHideCts.Token);
                    ShowStallCard = false;
                    StatusText    = "🔍 Đang quét GPS... Hãy đi dạo!";
                }
                catch (OperationCanceledException)
                {
                    // Một sạp mới đã trigger trước khi hết 8s → không cần hide
                }
            });
        }

        // ═══════════════════════════════════════════════════════════════
        //  CLEANUP
        // ═══════════════════════════════════════════════════════════════

        public async ValueTask DisposeAsync()
        {
            _discoveryService.StallEntered -= OnStallEntered;
            _cardHideCts?.Cancel();
            _cardHideCts?.Dispose();

            if (_discoveryService.IsRunning)
                await _discoveryService.StopAsync();
        }
    }
}
