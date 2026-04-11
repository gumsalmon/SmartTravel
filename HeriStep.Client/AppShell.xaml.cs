using HeriStep.Client.Services;

namespace HeriStep.Client;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Apply localized tab titles
        ApplyLocalization();

        // Refresh tab titles whenever the user changes language
        L.LanguageChanged += ApplyLocalization;

        // Hook offline detection globally without restarting app
        Microsoft.Maui.Networking.Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess != NetworkAccess.Internet)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await CommunityToolkit.Maui.Alerts.Toast.Make("Mất kết nối mạng. Ứng dụng đã chuyển sang chế độ ngoại tuyến (Offline).").Show();
                }
                catch { }
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await CommunityToolkit.Maui.Alerts.Toast.Make("Đã có kết nối Internet trở lại!").Show();
                }
                catch { }
            });
        }
    }

    private void ApplyLocalization()
    {
        tabExplore.Title = L.Get("tab_explore");
        tabMap.Title = L.Get("tab_map");
        tabProfile.Title = L.Get("tab_profile");
    }

}