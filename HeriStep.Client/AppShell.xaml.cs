using HeriStep.Client.Services;

namespace HeriStep.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register push-navigation routes (pages opened via Navigation.PushAsync)
        Routing.RegisterRoute(nameof(Views.ShopDetailPage), typeof(Views.ShopDetailPage));
        Routing.RegisterRoute(nameof(Views.FilterResultPage), typeof(Views.FilterResultPage));
        Routing.RegisterRoute(nameof(Views.LanguagePage), typeof(Views.LanguagePage));

        // Apply localized tab titles
        ApplyLocalization();

        // Refresh tab titles whenever the user changes language
        L.LanguageChanged += ApplyLocalization;
    }

    private void ApplyLocalization()
    {
        tabExplore.Title = L.Get("tab_explore");
        tabMap.Title = L.Get("tab_map");
        tabProfile.Title = L.Get("tab_profile");
    }
}