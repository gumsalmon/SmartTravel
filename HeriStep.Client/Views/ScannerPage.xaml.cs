using System;

namespace HeriStep.Client.Views
{
    public partial class ScannerPage : ContentPage
    {
        public ScannerPage()
        {
            InitializeComponent();
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ProfilePage");
        }

        private async void OnManualCodeClicked(object sender, EventArgs e)
        {
            var code = await DisplayPromptAsync("Nhập mã QR", "Nhập mã hoặc nội dung bạn muốn quét thử:", "OK", "Hủy", "VD: TC-AB12CD34");
            if (!string.IsNullOrWhiteSpace(code))
            {
                await DisplayAlert("Mã đã nhập", $"Bạn vừa nhập: {code}", "Đóng");
            }
        }

        private async void OnFlashTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Đèn pin", "Ở bản demo, đèn pin chỉ là minh họa. Khi triển khai thật sẽ dùng Camera/Flash của thiết bị.", "Đã hiểu");
        }

        private async void OnGalleryTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Chọn ảnh từ thư viện", "Tính năng quét QR từ ảnh sẽ được tích hợp sau. Hiện tại đây là màn hình mô phỏng UI.", "OK");
        }
    }
}
