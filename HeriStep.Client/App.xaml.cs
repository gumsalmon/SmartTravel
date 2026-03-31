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
        bool hasSelectedLanguage = Microsoft.Maui.Storage.Preferences.Default.Get("has_selected_language", false);
        
        if (!hasSelectedLanguage)
        {
            return new Window(new HeriStep.Client.Views.LanguagePage());
        }

        // Khởi tạo AppShell để hiện thanh TabBar cho đồ án HeriStep
        return new Window(new AppShell());
    }
}