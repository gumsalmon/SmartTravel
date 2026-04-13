using System;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly AudioTranslationService _audioService;
        private Action? _langChangedHandler;

        public ProfilePage(SubscriptionService subscriptionService, AudioTranslationService audioService)
        {
            InitializeComponent();
            _subscriptionService = subscriptionService;
            _audioService = audioService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ApplyLocalization();

            // Subscribe once to language changes
            if (_langChangedHandler == null)
            {
                _langChangedHandler = () => MainThread.BeginInvokeOnMainThread(ApplyLocalization);
                L.LanguageChanged += _langChangedHandler;
            }

            // Device ID
            lblDeviceId.Text = $"{L.Get("profile_device")} {_subscriptionService.GetDeviceId()}";

            // Subscription status
            var status = await _subscriptionService.CheckStatusAsync();
            if (status != null && status.Valid)
            {
                var hrs = status.RemainingHours ?? 0;
                var days = (int)Math.Floor(hrs / 24);
                var hours = (int)Math.Floor(hrs % 24);
                lblTimeRemaining.Text = string.Format(L.Get("profile_expiry_ok"), days, hours);
                lblTimeRemaining.TextColor = Colors.Gold;
            }
            else
            {
                lblTimeRemaining.Text = L.Get("profile_expiry_expired");
                lblTimeRemaining.TextColor = Colors.IndianRed;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_langChangedHandler != null)
            {
                L.LanguageChanged -= _langChangedHandler;
                _langChangedHandler = null;
            }
        }

        private void ApplyLocalization()
        {
            lblProfileTitle.Text      = L.Get("profile_title");
            lblProfileDisplayName.Text= L.Get("profile_display_name");
            lblStatVisited.Text       = L.Get("profile_visited");
            lblStatSaved.Text         = L.Get("profile_saved_lbl");
            lblStatRating.Text        = L.Get("profile_rating");
            lblChangeLang.Text        = L.Get("profile_change_lang");
            lblChangeLangDesc.Text    = L.Get("profile_lang_desc");
            lblSaved.Text             = L.Get("profile_saved_lbl");
            lblSavedCount.Text        = L.Get("profile_saved_count");
            lblSupport.Text           = L.Get("profile_support");
            lblSupportDesc.Text       = L.Get("profile_support_24");
            lblChangePkg.Text         = L.Get("profile_logout");
            lblChangePkgDesc.Text     = L.Get("profile_logout_desc");
            lblRecentHistory.Text     = L.Get("profile_history");
            lblViewAll.Text           = L.Get("profile_view_all");
            // History items
            lblHistoryItem1.Text      = L.Get("profile_history_item1");
            lblHistoryTag1.Text       = L.Get("profile_history_tag1");
            lblHistoryItem2.Text      = L.Get("profile_history_item2");
            lblHistoryTag2.Text       = L.Get("profile_history_tag2");
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnSavedSpotsTapped(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("notification"), "Khu vực địa điểm đã lưu đang được cập nhật.", L.Get("ok"));
        }

        private async void OnSupportTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Email: support@heristep.local\nHotline: 1800-HERISTEP", L.Get("ok"));
        }

        private async void OnViewAllHistoryTapped(object sender, EventArgs e)
        {
            await DisplayAlert(L.Get("profile_history"), "Full history view coming soon.", L.Get("ok"));
        }

        private async void OnChangePackageTapped(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert(
                L.Get("notification"),
                L.Get("profile_logout_confirm"),
                L.Get("ok"), L.Get("close"));

            if (confirm)
            {
                // 🗑️ Xóa dữ liệu đăng ký cũ để buộc app quay về trang QR
                Preferences.Default.Remove("device_ticket_id");
                Preferences.Default.Remove("ticket_expiry");
                Preferences.Default.Remove("jwt_token");

                // 🔄 Chuyển về trang chọn gói (SubscriptionPage) để quét lại QR
                Application.Current!.MainPage = new NavigationPage(new SubscriptionPage(_subscriptionService));
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
            await Navigation.PushAsync(new LanguagePage(isChangeMode: true));
        }
    }
}
