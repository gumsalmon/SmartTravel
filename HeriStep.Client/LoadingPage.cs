using System;
using Microsoft.Maui.Controls;
using HeriStep.Client.Services;
using HeriStep.Client.Views;

namespace HeriStep.Client;

public class LoadingPage : ContentPage
{
    private readonly SubscriptionService _subscriptionService;
    private readonly AudioTranslationService _audioService;

    public LoadingPage(SubscriptionService subscriptionService, AudioTranslationService audioService)
    {
        _subscriptionService = subscriptionService;
        _audioService = audioService;
        BackgroundColor = Color.FromArgb("#121212");
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children = 
            {
                new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#FF8C00") },
                new Label { Text = "Đang kiểm tra thông tin...", TextColor = Colors.White, Margin = new Thickness(0, 10, 0, 0) }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _audioService.WarmUpAsync(); // Không đợi (non-blocking) nhưng bắt đầu chạy ngay
        await CheckSubscription();
    }

    private async Task CheckSubscription()
    {
        var hasLanguage = Microsoft.Maui.Storage.Preferences.Default.ContainsKey("user_language");
        if (!hasLanguage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new LanguageSelectionPage(_subscriptionService, _audioService);
            });
            return;
        }

        try 
        {
            // BƯỚC 1: Kiểm tra local cache trước (nhanh, không cần mạng)
            var offlineExpiryStr = Microsoft.Maui.Storage.Preferences.Default.Get("sub_expires_at", "");
            bool localExpired = true;

            if (DateTime.TryParse(offlineExpiryStr, out DateTime localExpiry))
            {
                localExpired = localExpiry <= DateTime.UtcNow;
            }

            if (!localExpired)
            {
                // Local cache còn hạn → vào AppShell bình thường (không cần gọi API)
                Console.WriteLine("[LOADING] Local cache còn hạn. Vào AppShell.");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage = new AppShell();
                });
                return;
            }

            // BƯỚC 2: Local báo hết hạn → Double-check với Server để chống time-hack
            // (User có thể chỉnh lùi giờ điện thoại để giả vờ còn hạn)
            Console.WriteLine("[LOADING] Local expired. Gọi server double-check chống time-hack...");
            var serverCheck = await _subscriptionService.CheckServerSubscriptionAsync();

            if (serverCheck != null && !serverCheck.IsExpired)
            {
                // SERVER NÓI CÒN HẠN → Đây là time-hack (user chỉnh lùi giờ)
                // Cập nhật lại local cache theo giờ server thực tế
                Console.WriteLine("[TIME_HACK_DETECTED] Server nói còn hạn, nhưng local nói hết hạn. Cập nhật cache.");
                if (serverCheck.ExpiryDate.HasValue)
                {
                    Microsoft.Maui.Storage.Preferences.Default.Set(
                        "sub_expires_at", 
                        serverCheck.ExpiryDate.Value.ToUniversalTime().ToString("o"));
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current.MainPage = new AppShell();
                });
                return;
            }

            // BƯỚC 3: Nếu không có mạng (serverCheck == null) → Kiểm tra lần cuối qua API cũ
            if (serverCheck == null)
            {
                Console.WriteLine("[LOADING] Server không trả lời. Kiểm tra API cũ...");
                var status = await _subscriptionService.CheckStatusAsync();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (status != null && status.Valid)
                    {
                        Application.Current.MainPage = new AppShell();
                    }
                    else
                    {
                        // HẾT HẠN THỰC SỰ hoặc chưa đăng ký → Renewal/Subscription
                        Application.Current.MainPage = new RenewalPage(_subscriptionService);
                    }
                });
                return;
            }

            // BƯỚC 4: Server confirms expired → Chuyển sang RenewalPage (gia hạn)
            Console.WriteLine("[LOADING] Server xác nhận hết hạn. Chuyển sang RenewalPage.");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new RenewalPage(_subscriptionService);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Error in LoadingPage: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Fallback to AppShell if we can't check subscription (allows offline usage)
                await DisplayAlert("Thông báo", "Không thể kết nối máy chủ. Ứng dụng sẽ hoạt động ở chế độ ngoại tuyến.", "OK");
                Application.Current.MainPage = new AppShell();
            });
        }
    }
}
