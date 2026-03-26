using HeriStep.Client.Services;
using HeriStep.Client.Services.Location; // Thêm cái này
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

        // 1. Cấu hình HttpClient (Lưu ý: 10.0.2.2 chỉ dùng cho Android Emulator)
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5297/")
        });

        // 2. Đăng ký Services
        builder.Services.AddSingleton<ShopService>();

        // 💡 Đã dùng Alias gọn gàng hơn nhờ using ở trên
#if DEBUG
        builder.Services.AddSingleton<ILocationService, MockLocationService>();
#else
        builder.Services.AddSingleton<ILocationService, RealLocationService>();
#endif

        // 3. Đăng ký ViewModels (Đăng ký Singleton nếu muốn giữ trạng thái, Transient nếu muốn reset)
        builder.Services.AddSingleton<HomeViewModel>();
        // builder.Services.AddTransient<LoginViewModel>(); // Sếp nên thêm dòng này nếu có LoginViewModel

        // 4. Đăng ký Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}