namespace HeriStep.Client.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnGoogleClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Google Login", "Bản demo chưa tích hợp OAuth Google. Vui lòng dùng Explore as Guest.", "OK");
    }

    private async void OnAppleClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Apple Login", "Bản demo chưa tích hợp Sign in with Apple. Vui lòng dùng Explore as Guest.", "OK");
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Forgot Password", "Tính năng đặt lại mật khẩu sẽ được kết nối API tài khoản sau.", "Đóng");
    }

    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Sign Up", "Màn hình đăng ký sẽ được bổ sung ở bản kế tiếp.", "Đóng");
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Demo: dùng cùng flow guest để người dùng vào được app ngay
        BtnGuest_Clicked(sender, e);
    }

    private async void BtnGuest_Clicked(object sender, EventArgs e)
    {
        // 💳 TICKET GATE: Kiểm tra vé đã được lưu chưa
        string savedTicket = Microsoft.Maui.Storage.Preferences.Default.Get("tourist_ticket", string.Empty);

        if (!string.IsNullOrEmpty(savedTicket))
        {
            // Đã có vé → vào thẳng Trang chủ (MainPage trong Shell)
            await Shell.Current.GoToAsync("//MainPage");
            return;
        }

        // Chưa có vé → Hỏi nhập mã hoặc vào freemium
        string? ticketCode = await DisplayPromptAsync(
            "🎫 Mã Vé Tham Quan",
            "Nhập mã vé để mở khóa tính năng đầy đủ.\nHoặc để trống để khám phá miễn phí (chỉ xem danh sách).",
            placeholder: "VD: TC-AB12CD34",
            maxLength: 20
        );

        if (!string.IsNullOrWhiteSpace(ticketCode))
        {
            // Có nhập mã → Validate với API
            bool isValid = await ValidateTicketWithApi(ticketCode.Trim().ToUpper());
            if (isValid)
            {
                Microsoft.Maui.Storage.Preferences.Default.Set("tourist_ticket", ticketCode.Trim().ToUpper());
                await DisplayAlert("✅ Kích Hoạt Thành Công!", "Vé của bạn đã được xác nhận. Chào mừng đến Phố Ẩm Thực Vĩnh Khánh!", "Khám phá ngay!");
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await DisplayAlert("❌ Mã Không Hợp Lệ", "Mã vé không tồn tại hoặc đã hết hạn. Vui lòng kiểm tra lại.", "Thử lại");
            }
        }
        else
        {
            // Không nhập vé → Vẫn cho vào nhưng lưu flag freemium
            Microsoft.Maui.Storage.Preferences.Default.Set("is_freemium", true);
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private async Task<bool> ValidateTicketWithApi(string code)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"http://10.0.2.2:5297/api/Tickets/validate/{code}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Nếu API chưa có endpoint này → cho qua (chế độ dev)
            return true;
        }
    }
}
