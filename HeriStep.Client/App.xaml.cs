using HeriStep.Client.Services;

namespace HeriStep.Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Initialize the localization service from saved preferences
        L.Init();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loadingPage = activationState?.Context.Services.GetService<LoadingPage>();
        if (loadingPage != null) return new Window(loadingPage);

        var subService = activationState?.Context.Services.GetService<SubscriptionService>() ?? new SubscriptionService();
        var audioService = activationState?.Context.Services.GetService<AudioTranslationService>();
        
        // Note: AudioTranslationService is mandatory now for LoadingPage
        return new Window(new LoadingPage(subService, audioService!));
    }
}