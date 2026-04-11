using System;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly AudioTranslationService _audioService;

        public ProfilePage(SubscriptionService subscriptionService, AudioTranslationService audioService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
            _audioService = audioService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            lblDeviceId.Text = $"Mã máy: {_subscriptionService.GetDeviceId()}";

            var status = await _subscriptionService.CheckStatusAsync();
            if (status != null && status.Valid)
            {
                var hrs = status.RemainingHours ?? 0;
                var days = Math.Floor(hrs / 24);
                var hours = Math.Floor(hrs % 24);
                lblTimeRemaining.Text = $"Hạn sử dụng: CÒN LẠI {days} Ngày {hours} Giờ";
            }
            else
            {
                lblTimeRemaining.Text = "Hạn sử dụng: ĐÃ HẾT HẠN";
                lblTimeRemaining.TextColor = Colors.Red;
            }
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnSavedSpotsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Khu vực địa điểm đã lưu đang được cập nhật.", "OK");
        }

        private async void OnSupportTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Email: support@heristep.local\nHotline: 1800-HERISTEP", "OK");
        }

        private async void OnViewAllHistoryTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Visit History", "Full history view coming soon.", "OK");
        }

        private async void OnChangePackageTapped(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Xác nhận", "Bạn có thực sự muốn đăng xuất để test lại chu trình đăng ký gói không?", "Đồng ý", "Hủy");
            if (confirm)
            {
                // Cho phép reset hoàn toàn tài khoản và ngôn ngữ để kiểm tra Full UI Logic
                Microsoft.Maui.Storage.Preferences.Default.Remove("device_uuid");
                Microsoft.Maui.Storage.Preferences.Default.Remove("user_language");
                
                Application.Current.MainPage = new LoadingPage(new SubscriptionService());
            }
        }

        private async void OnHistoryItem1Tapped(object sender, EventArgs e)
        {
            var stall = new Stall
            {
                Id = 11,
                Name = "Oc Dao Vinh Khanh",
                ImageUrl = "https://images.unsplash.com/photo-1544025162-8e658402afb0?w=600"
            };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall, _audioService));
        }

        private async void OnHistoryItem2Tapped(object sender, EventArgs e)
        {
            var stall = new Stall
            {
                Id = 12,
                Name = "Mrs. Sau's Rolls",
                ImageUrl = "https://images.unsplash.com/photo-1574484284002-952d92456975?w=600"
            };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall, _audioService));
        }

        private async void OnChangeLanguageTapped(object sender, EventArgs e)
        {
            // Push instead of replacing root — preserves AppShell + ViewModel + TTS state
            await Navigation.PushAsync(new LanguagePage(isChangeMode: true));
        }
    }
}
