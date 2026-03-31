using System;
using HeriStep.Client.Views;

namespace HeriStep.Client
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            // Mở tab Cá nhân
            await Shell.Current.GoToAsync("//ProfilePage");
        }

        private async void OnAvatarTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }

        private async void BtnMap_Clicked(object sender, EventArgs e)
        {
            // Điều hướng tới trang bản đồ trong Shell
            await Shell.Current.GoToAsync(nameof(Views.MapPage));
        }

        private async void BtnShop_Clicked(object sender, EventArgs e)
        {
            // Mở trang chi tiết quán với dữ liệu mock
            var stall = new HeriStep.Shared.Models.Stall
            {
                Id = 1,
                Name = "Ốc Oanh Vĩnh Khánh",
                ImageUrl = "https://images.unsplash.com/photo-1548690312-e3b507d8c110?w=600"
            };

            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }

        private async void BtnShop2_Clicked(object sender, EventArgs e)
        {
            var stall = new HeriStep.Shared.Models.Stall
            {
                Id = 2,
                Name = "Bà Kẹp Vĩnh Khánh",
                ImageUrl = "https://images.unsplash.com/photo-1574484284002-952d92456975?w=600"
            };

            await Shell.Current.Navigation.PushAsync(new ShopDetailPage(stall));
        }

        private async void BtnScan_Clicked(object sender, EventArgs e)
        {
            // Chuyển sang tab Quét QR
            await Shell.Current.GoToAsync("//ScannerPage");
        }

        private async void BtnVoiceAura_Clicked(object sender, EventArgs e)
        {
            // Chuyển sang tab Voice Aura
            await Shell.Current.GoToAsync("//VoiceAuraPage");
        }

        private async void BtnActivateAura_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//VoiceAuraPage");
            await DisplayAlert("Voice Aura", "Tính năng Voice Aura sẽ tự động hướng dẫn khi bạn ở gần các điểm tham quan hỗ trợ.", "Đã hiểu");
        }

        private async void Pass1_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new Views.PaymentPage());
        }

        private async void Pass2_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new Views.PaymentPage());
        }

        private async void Pass3_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new Views.PaymentPage());
        }
    }
}