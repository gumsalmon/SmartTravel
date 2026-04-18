using HeriStep.Client.Services;

namespace HeriStep.Client;

public partial class App : Application
{
    private readonly SignalRService _signalR;

    public App(SignalRService signalR)
    {
        _signalR = signalR;
        InitializeComponent();

        // Khởi tạo localization
        L.Init();

        // Kết nối SignalR ngầm ngay khi App bật
        // _ = để fire-and-forget (không block UI thread)
        _ = _signalR.ConnectAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loadingPage = activationState?.Context.Services.GetService<LoadingPage>();
        if (loadingPage != null) return new Window(loadingPage);

        var subService   = activationState?.Context.Services.GetService<SubscriptionService>() ?? new SubscriptionService();
        var audioService = activationState?.Context.Services.GetService<AudioTranslationService>();

        return new Window(new LoadingPage(subService, audioService!));
    }

    // Khi App bị kill bởi OS (low memory, user swipe away)
    // .NET MAUI gọi OnSleep rồi process bị hủy — TCP tự đứt → Hub -1
    protected override void OnSleep()
    {
        base.OnSleep();
        // Không cần gọi DisconnectAsync() vì OS kill process → TCP đứt tự nhiên
        // Hub ClientTimeoutInterval=60s sẽ phát hiện và gọi OnDisconnectedAsync
    }
}