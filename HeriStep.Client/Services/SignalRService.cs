using Microsoft.AspNetCore.SignalR.Client;

namespace HeriStep.Client.Services
{
    /// <summary>
    /// Kết nối ẩn danh vào DashboardHub trên Admin Web.
    /// Mỗi khi App mở → Hub đếm +1.
    /// Khi App đóng / bị kill → TCP ngắt → Hub tự đếm -1.
    /// </summary>
    public class SignalRService
    {
        private HubConnection? _connection;
        private bool _isConnected;

        // ─────────────────────────────────────────────────────────
        // KẾT NỐI — gọi từ App.xaml.cs khi App khởi động
        // ─────────────────────────────────────────────────────────
        public async Task ConnectAsync()
        {
            if (_isConnected) return;

            // URL tự đổi theo platform: emulator dùng 10.0.2.2, còn lại dùng localhost
            var hubUrl = AppConstants.AdminHubUrl;

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // Không gửi cookies / credential — App Client là anonymous
                    options.UseDefaultCredentials = false;
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2),
                                                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10),
                                                TimeSpan.FromSeconds(30) })
                .Build();

            _connection.Closed += async (error) =>
            {
                _isConnected = false;
                System.Diagnostics.Debug.WriteLine($"[SignalR] Kết nối đóng: {error?.Message}");
                await Task.Delay(5000);
                await ConnectAsync(); // tự thử lại
            };

            _connection.Reconnected += (connectionId) =>
            {
                _isConnected = true;
                System.Diagnostics.Debug.WriteLine($"[SignalR] Đã reconnect: {connectionId}");
                return Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync();
                _isConnected = true;
                System.Diagnostics.Debug.WriteLine($"[SignalR] ✅ App kết nối Hub → Admin +1 | URL: {hubUrl}");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                System.Diagnostics.Debug.WriteLine($"[SignalR] ❌ Thất bại: {ex.Message} — thử lại sau 10s");
                await Task.Delay(10_000);
                await ConnectAsync();
            }
        }

        // ─────────────────────────────────────────────────────────
        // NGẮT KẾT NỐI — gọi khi logout (thường không cần vì TCP sẽ tự đứt)
        // ─────────────────────────────────────────────────────────
        public async Task DisconnectAsync()
        {
            if (_connection is not null && _isConnected)
            {
                await _connection.StopAsync();
                _isConnected = false;
                System.Diagnostics.Debug.WriteLine("[SignalR] 🔴 App ngắt kết nối Hub → Admin -1");
            }
        }

        public bool IsConnected => _isConnected;
    }
}
