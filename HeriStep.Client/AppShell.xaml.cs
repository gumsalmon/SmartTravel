namespace HeriStep.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký đường dẫn cho các trang để dễ điều hướng
        Routing.RegisterRoute(nameof(Views.HomePage), typeof(Views.HomePage));
        Routing.RegisterRoute(nameof(Views.MapPage), typeof(Views.MapPage));

        // ✅ FIX CRASH BACK: Đăng ký ShopDetailPage như một route có thể navigate đến
        // Shell cần biết về page này mới không crash khi dùng Navigation.PushAsync / PopAsync
        Routing.RegisterRoute(nameof(Views.ShopDetailPage), typeof(Views.ShopDetailPage));
    }
}