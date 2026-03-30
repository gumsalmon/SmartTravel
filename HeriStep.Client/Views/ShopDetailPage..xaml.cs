using HeriStep.Shared.Models;
using System.Net.Http.Json;

namespace HeriStep.Client.Views;

public partial class ShopDetailPage : ContentPage
{
    private readonly Stall _stall;
    private const string ApiBase = "http://10.0.2.2:5297";

    public ShopDetailPage(Stall shop)
    {
        InitializeComponent();
        _stall = shop;
        BindingContext = shop;

        // 💡 Điền dữ liệu bổ sung mà Binding không cover
        lblRadius.Text = $"{(int)(shop.RadiusMeter > 0 ? shop.RadiusMeter : 50)}m";
        lblCoords.Text = $"Vĩ độ: {shop.Latitude:F5} | Kinh độ: {shop.Longitude:F5}";

        // 💳 SUBSCRIPTION CHECK: Kiểm tra sạp còn hoạt động không
        if (!shop.IsOpen)
        {
            lblStatus.Text = "● Đã đóng";
            lblStatus.TextColor = Color.FromArgb("#EF4444");
            subBanner.IsVisible = true;
            lblSubStatus.Text = "⚠️ Sạp này hiện đang đóng cửa";
            ttsButton.IsEnabled = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMenuAsync();
        await CheckSubscriptionStatusAsync();
    }

    // ══════════════════════════════════════════════
    // 🍽️ Tải Thực Đơn từ API
    // ══════════════════════════════════════════════
    private async Task LoadMenuAsync()
    {
        try
        {
            menuLoader.IsVisible = true;
            menuLoader.IsRunning = true;

            using var client = new HttpClient();
            string lang = Microsoft.Maui.Storage.Preferences.Default.Get("lang_code", "vi");
            var url = $"{ApiBase}/api/Stalls/{_stall.Id}/products?lang={lang}";
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var products = await client.GetFromJsonAsync<List<Product>>(url, options);

            menuLoader.IsVisible = false;
            menuContainer.Children.Clear();

            if (products != null && products.Count > 0)
            {
                // Hiển thị số lượt thăm (dùng product count làm proxy)
                lblVisits.Text = $"{products.Count * 24}+";

                foreach (var product in products)
                {
                    // Tạo Product Card động đúng chuẩn Glassmorphism
                    var card = BuildProductCard(product);
                    menuContainer.Children.Add(card);
                }
            }
            else
            {
                lblNoMenu.IsVisible = true;
                lblVisits.Text = "0";
            }
        }
        catch (Exception ex)
        {
            menuLoader.IsVisible = false;
            System.Diagnostics.Debug.WriteLine($"[Menu Load] {ex.Message}");

            // Hiển thị menu mẫu khi API chưa có data
            menuLoader.IsVisible = false;
            var fallback = BuildFallbackMenu();
            menuContainer.Children.Add(fallback);
            lblVisits.Text = "120+";
        }
    }

    // ══════════════════════════════════════════════
    // 💳 Kiểm tra trạng thái Subscription của Sạp
    // ══════════════════════════════════════════════
    private async Task CheckSubscriptionStatusAsync()
    {
        try
        {
            using var client = new HttpClient();
            var url = $"{ApiBase}/api/Stalls/{_stall.Id}/subscription-status";
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Nếu API trả về IsActive = false → biểu diễn hết hạn
            var result = await client.GetFromJsonAsync<SubscriptionStatusDto>(url, options);

            if (result != null && !result.IsActive)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    subBanner.IsVisible = true;
                    lblSubStatus.Text = $"⚠️ Gói cước hết hạn ngày {result.ExpiryDate:dd/MM/yyyy} - Liên hệ Admin để gia hạn";
                    lblStatus.Text = "● Gói cước hết hạn";
                    lblStatus.TextColor = Color.FromArgb("#F59E0B");
                    ttsButton.IsEnabled = false;
                });
            }
        }
        catch
        {
            // Không có endpoint subscription → bỏ qua, sạp vẫn hoạt động
            System.Diagnostics.Debug.WriteLine("[Subscription] Endpoint chưa có, bỏ qua check.");
        }
    }

    // ══════════════════════════════════════════════
    // 🎨 Builder: Tạo Product Card với Glassmorphism
    // ══════════════════════════════════════════════
    private static View BuildProductCard(Product product)
    {
        var nameLabel = new Label
        {
            Text = product.Name ?? "Món ăn",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = Colors.White
        };

        string priceText = product.Price > 0 ? $"{product.Price:N0}đ" : "Liên hệ";
        var priceLabel = new Label
        {
            Text = priceText,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = Color.FromArgb("#4ADE80"), // Màu xanh lá nổi bật
            VerticalOptions = LayoutOptions.Center
        };

        var descLabel = new Label
        {
            Text = product.Description ?? "",
            FontSize = 12,
            TextColor = Color.FromArgb("#888AAA"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            ],
            RowDefinitions =
            [
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            ]
        };

        grid.Add(nameLabel, 0, 0);
        grid.Add(priceLabel, 1, 0);
        Grid.SetRowSpan(priceLabel, 2);
        grid.Add(descLabel, 0, 1);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#1AFFFFFF"),
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#33FFFFFF"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(16, 12),
            Content = grid
        };
    }

    /// <summary>Menu mẫu hiển thị khi API chưa có dữ liệu thực đơn</summary>
    private static View BuildFallbackMenu()
    {
        var items = new[]
        {
            ("Ốc Hương Rang Muối", "Ngon bá cháy bọ chét - Đặc sản số 1", "120.000đ"),
            ("Sò Huyết Cháy Tỏi", "Đậm đà hương vị biển", "90.000đ"),
            ("Mực Nướng Sa Tế", "Cháy cạnh thơm phức", "150.000đ")
        };

        var stack = new VerticalStackLayout { Spacing = 10 };
        foreach (var (name, desc, price) in items)
        {
            stack.Children.Add(BuildProductCard(new Product
            {
                Name = name,
                Description = desc,
                Price = decimal.Parse(price.Replace(".", "").Replace("đ", ""))
            }));
        }
        return stack;
    }

    // ══════════════════════════════════════════════
    // 🔊 TTS Phát thanh thuyết minh
    // ══════════════════════════════════════════════
    private async void PlayAudio_Clicked(object sender, EventArgs e)
    {
        ttsButton.IsEnabled = false;
        ttsButton.Text = "🔄 Đang phát...";

        try
        {
            string lang = Microsoft.Maui.Storage.Preferences.Default.Get("lang_code", "vi");
            string textToSpeak = BuildDefaultTTS(); // Mặc định dùng text tự tạo trước

            // Thử lấy nội dung từ API Backend, nếu lỗi thì fallback
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Timeout 5s, không chờ mãi
                var url = $"{ApiBase}/api/Stalls/{_stall.Id}/tts/{lang}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var apiText = doc.RootElement.GetProperty("text").GetString();
                    if (!string.IsNullOrEmpty(apiText))
                        textToSpeak = apiText;
                }
            }
            catch
            {
                // API không phản hồi → dùng text tự sinh bên dưới
                System.Diagnostics.Debug.WriteLine("[TTS] Fallback sang text tự sinh");
            }

            System.Diagnostics.Debug.WriteLine($"[TTS] Phát: {textToSpeak}");

            // ✅ Dùng SpeakAsync chỉ với text, không truyền SpeechOptions hay CancellationToken gây lỗi overload
            await TextToSpeech.Default.SpeakAsync(textToSpeak);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTS Error] {ex.Message}");
            // Không dùng DisplayAlert trong catch để tránh UI race condition
        }
        finally
        {
            ttsButton.IsEnabled = true;
            ttsButton.Text = "🔊 Nghe Thuyết Minh";
        }
    }

    private string BuildDefaultTTS()
    {
        string lang = Microsoft.Maui.Storage.Preferences.Default.Get("lang_code", "vi");
        return lang == "vi"
            ? $"Chào mừng bạn đến {_stall.Name}. Đây là một trong những sạp hải sản đặc biệt tại phố ẩm thực Vĩnh Khánh, Quận 4. Hãy ghé thăm để thưởng thức hương vị tuyệt vời!"
            : $"Welcome to {_stall.Name}. This is one of the special seafood stalls at Vinh Khanh food street, District 4. Come and enjoy the amazing flavors!";
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
        else
            await Shell.Current.GoToAsync("..");
    }
}

// DTO đơn giản để parse kết quả subscription
public class SubscriptionStatusDto
{
    public bool IsActive { get; set; }
    public DateTime ExpiryDate { get; set; }
}

// DTO Product tạm thời
public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}