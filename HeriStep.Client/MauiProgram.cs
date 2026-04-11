using HeriStep.Client.Services;
using HeriStep.Client.Services.Location;
using HeriStep.Client.ViewModels;
using HeriStep.Client.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using CommunityToolkit.Maui;

namespace HeriStep.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkit()
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
        builder.Services.AddSingleton<SubscriptionService>();
        builder.Services.AddSingleton<LocalDatabaseService>();
        builder.Services.AddSingleton<AudioTranslationService>();

#if DEBUG
        builder.Services.AddSingleton<ILocationService, MockLocationService>();
#else
        builder.Services.AddSingleton<ILocationService, RealLocationService>();
#endif

        // ═══ VIEWMODELS ═══
        builder.Services.AddSingleton<HomeViewModel>();

        // ═══ PAGES ═══
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<SubscriptionPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<HomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}