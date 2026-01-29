namespace HeriStep.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký đường dẫn cho các trang để dễ điều hướng
        Routing.RegisterRoute(nameof(Views.HomePage), typeof(Views.HomePage));
    }
}