using HeriStep.Client.Services;
using HeriStep.Client.ViewModels;
using HeriStep.Client.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace HeriStep.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 1. ĐÃ FIX: Bỏ chữ "api/" ở đuôi để không bị nối trùng lặp
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5297/")
        });

        // 2. Đăng ký các dịch vụ và Giao diện
        builder.Services.AddSingleton<ShopService>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<MainPage>();

        // 3. ĐÃ FIX: Dời dòng này lên TRÊN chữ return để App có thể chạy được trang Login
        builder.Services.AddTransient<LoginPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        // Dòng chốt sổ bắt buộc phải nằm ở sát đáy!
        return builder.Build();
    }
}