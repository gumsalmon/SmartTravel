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
        return new Window(loadingPage ?? new LoadingPage(new SubscriptionService()));
    }
}