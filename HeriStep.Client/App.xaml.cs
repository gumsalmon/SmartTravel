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
        // First launch: show LanguagePage to pick language
        bool hasSelectedLanguage = Microsoft.Maui.Storage.Preferences.Default.Get("has_selected_language", false);

        if (!hasSelectedLanguage)
        {
            return new Window(new HeriStep.Client.Views.LanguagePage());
        }

        // Normal launch: go straight to the main app shell
        return new Window(new AppShell());
    }
}