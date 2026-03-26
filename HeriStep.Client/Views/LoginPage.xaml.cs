namespace HeriStep.Client.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    // Hàm này sẽ tự động chạy khi khách bấm nút Guest
    private async void BtnGuest_Clicked(object sender, EventArgs e)
    {
        // Hiển thị thông báo chào mừng nhẹ nhàng
        if (Shell.Current != null) await Shell.Current.DisplayAlert("Welcome", "Đang chuẩn bị hành trang đến phố ốc Vĩnh Khánh...", "Let's go!");
        await Shell.Current.GoToAsync("//HomePage");
    }
}