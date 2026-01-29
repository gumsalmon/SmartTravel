using Microsoft.Extensions.DependencyInjection;

namespace HeriStep.Client;

public partial class App : Application
{
    // Đây là hàm khởi tạo duy nhất được phép tồn tại
    public App()
    {
        InitializeComponent();
    }

    // Đây là hàm tạo cửa sổ duy nhất được phép tồn tại (Chuẩn .NET 10)
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Khởi tạo AppShell để hiện thanh TabBar cho đồ án HeriStep
        return new Window(new AppShell());
    }
}