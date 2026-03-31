using System;
using HeriStep.Shared.Models;

namespace HeriStep.Client.Views
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        private async void OnConfigureClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//VoiceAuraPage");
        }

        private async void OnSavedSpotsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(MapPage));
        }

        private async void OnSupportTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Support", "Bạn có thể liên hệ hỗ trợ qua email: support@heristep.local", "Đóng");
        }

        private async void OnViewAllHistoryTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Lịch sử", "Lịch sử chi tiết sẽ được đồng bộ từ API trong phiên bản tiếp theo.", "OK");
        }

        private async void OnHistoryItem1Tapped(object sender, EventArgs e)
        {
            var stall = new Stall { Id = 11, Name = "Oc Dao Vinh Khanh", ImageUrl = "https://images.unsplash.com/photo-1544025162-8e658402afb0?w=600" };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }

        private async void OnHistoryItem2Tapped(object sender, EventArgs e)
        {
            var stall = new Stall { Id = 12, Name = "Mrs. Sau's Rolls", ImageUrl = "https://images.unsplash.com/photo-1574484284002-952d92456975?w=600" };
            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }
    }
}
