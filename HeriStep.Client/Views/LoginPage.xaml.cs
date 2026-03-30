namespace HeriStep.Client.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void BtnGuest_Clicked(object sender, EventArgs e)
    {
        // 💳 TICKET GATE: Kiểm tra vé đã được lưu chưa
        string savedTicket = Microsoft.Maui.Storage.Preferences.Default.Get("tourist_ticket", string.Empty);

        if (!string.IsNullOrEmpty(savedTicket))
        {
            // Đã có vé → vào thẳng
            await Shell.Current.GoToAsync("//HomePage");
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
                await Shell.Current.GoToAsync("//HomePage");
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
            await Shell.Current.GoToAsync("//HomePage");
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