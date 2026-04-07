using HeriStep.Client.Services;
using HeriStep.Client.Services.Location;
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

        // ═══ HTTP CLIENT ═══
        // Platform-aware base URL: 10.0.2.2 for Android emulator, localhost elsewhere
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(AppConstants.BaseApiUrl + "/")
        });

        // ═══ SERVICES ═══
        builder.Services.AddSingleton<ShopService>();
        builder.Services.AddSingleton<GeofenceService>();

#if DEBUG
        builder.Services.AddSingleton<ILocationService, MockLocationService>();
#else
        builder.Services.AddSingleton<ILocationService, RealLocationService>();
#endif

        // ═══ VIEWMODELS ═══
        builder.Services.AddSingleton<HomeViewModel>();

        // ═══ PAGES ═══
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<ProfilePage>();
        // Note: VoiceAuraPage removed — voice customization is now inline on MainPage

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}