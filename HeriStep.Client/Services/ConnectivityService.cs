using Microsoft.Maui.Networking;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// Centralized offline detection service.
    /// Listen to ConnectivityChanged to react in real-time.
    /// Usage: ConnectivityService.IsOnline, ConnectivityService.ConnectivityChanged
    /// </summary>
    public static class ConnectivityService
    {
        public static event Action<bool>? ConnectivityChanged;

        /// <summary>True if the device has internet access right now.</summary>
        public static bool IsOnline =>
            Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        /// <summary>Call once from MauiProgram to start listening.</summary>
        public static void Init()
        {
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        private static void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            var isOnline = e.NetworkAccess == NetworkAccess.Internet;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectivityChanged?.Invoke(isOnline);
            });
        }

        /// <summary>
        /// Returns true only when actually online.
        /// Shows an alert in the given page context when offline (optional).
        /// </summary>
        public static bool CheckAndAlert(Page? page = null)
        {
            if (IsOnline) return true;

            if (page != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await page.DisplayAlert(
                        "📶 " + Services.L.Get("offline_title"),
                        Services.L.Get("offline_msg"),
                        Services.L.Get("ok")
                    );
                });
            }

            return false;
        }
    }
}
