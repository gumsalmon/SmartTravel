using System;
using Microsoft.Maui.Controls;
using HeriStep.Client.Services;
using HeriStep.Client.Views;

namespace HeriStep.Client;

public class LoadingPage : ContentPage
{
    private readonly SubscriptionService _subscriptionService;

    public LoadingPage(SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
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
        await CheckSubscription();
    }

    private async Task CheckSubscription()
    {
        var hasLanguage = Microsoft.Maui.Storage.Preferences.Default.ContainsKey("user_language");
        if (!hasLanguage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new LanguageSelectionPage(_subscriptionService);
            });
            return;
        }

        try 
        {
            var status = await _subscriptionService.CheckStatusAsync();
            
            // Cần đảm bảo chạy trên UI thread nếu đổi MainPage
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (status == null || !status.Valid)
                {
                    Application.Current.MainPage = new SubscriptionPage(_subscriptionService);
                }
                else
                {
                    Application.Current.MainPage = new AppShell();
                }
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
