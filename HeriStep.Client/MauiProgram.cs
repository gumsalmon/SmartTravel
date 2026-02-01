using HeriStep.Client.Services; // Phải có thư mục Services
using HeriStep.Client.ViewModels;
using HeriStep.Client.Views; // Phải có thư mục Views
using Microsoft.Extensions.Logging;

namespace HeriStep.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Đăng ký HttpClient
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5297/api/")
        });

        // Đăng ký Logic (Nhớ tạo file tương ứng trước khi bỏ comment)
        builder.Services.AddSingleton<ShopService>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddTransient<HomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}