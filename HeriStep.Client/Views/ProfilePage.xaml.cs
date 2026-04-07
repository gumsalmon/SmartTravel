using System;
using HeriStep.Client.Services;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Views
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
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
            await DisplayAlertAsync("Support", "Email: support@heristep.local\nHotline: 1800-HERISTEP", "OK");
        }

        private async void OnViewAllHistoryTapped(object sender, EventArgs e)
        {
            await DisplayAlertAsync("Visit History", "Full history view coming soon.", "OK");
        }

        private async void OnHistoryItem1Tapped(object sender, EventArgs e)
        {
            var stall = new Stall
            {
                Id = 11,
                Name = "Oc Dao Vinh Khanh",
                ImageUrl = "https://images.unsplash.com/photo-1544025162-8e658402afb0?w=600"
            };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }

        private async void OnHistoryItem2Tapped(object sender, EventArgs e)
        {
            var stall = new Stall
            {
                Id = 12,
                Name = "Mrs. Sau's Rolls",
                ImageUrl = "https://images.unsplash.com/photo-1574484284002-952d92456975?w=600"
            };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }

        private async void OnChangeLanguageTapped(object sender, EventArgs e)
        {
            // Push instead of replacing root — preserves AppShell + ViewModel + TTS state
            await Navigation.PushAsync(new LanguagePage(isChangeMode: true));
        }
    }
}
