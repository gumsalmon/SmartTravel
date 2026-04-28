using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeriStep.Admin.Hubs
{
    /// <summary>
    /// Hub đếm khách đang dùng App (App Client — anonymous).
    /// 
    /// KIẾN TRÚC:
    ///   - Admin đã đăng nhập (IsAuthenticated = true) → vào group "Admins", chỉ nhận thông báo, KHÔNG bị đếm.
    ///   - App Client Flutter / WebClient ẩn danh       → được đếm vào _activeTourists.
    ///
    /// SỰ KIỆN GỬI VỀ FRONTEND:
    ///   - Tên: "UpdateActiveUsers"  (Admin Web lắng nghe tên này trong JS)
    /// </summary>
    public class DashboardHub : Hub
    {
        // Static + Interlocked = thread-safe, không cần lock
        private static int _activeTourists = 0;

        // ─────────────────────────────────────────────
        // KẾT NỐI MỚI
        // ─────────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            bool isAdmin = Context.User?.Identity?.IsAuthenticated == true;

            if (isAdmin)
            {
                // Admin vào xem Dashboard → thêm vào group riêng để nhận broadcast
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

                // Gửi ngay số hiện tại cho Admin vừa mở tab
                await Clients.Caller.SendAsync("UpdateActiveUsers", _activeTourists);
            }
            else
            {
                // Khách ẩn danh (App Client) mở app → +1
                int newCount = Interlocked.Add(ref _activeTourists, 2);

                // Broadcast tới tất cả Admin đang xem Dashboard
                await Clients.Group("Admins").SendAsync("UpdateActiveUsers", newCount);
            }

            await base.OnConnectedAsync();
        }

        // ─────────────────────────────────────────────
        // NGẮT KẾT NỐI
        // ─────────────────────────────────────────────
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            bool isAdmin = Context.User?.Identity?.IsAuthenticated == true;

            if (!isAdmin)
            {
                // Khách ẩn danh tắt app → -1 (không để xuống âm)
                int newCount = Math.Max(0, Interlocked.Add(ref _activeTourists, -2));

                // Đảm bảo biến tĩnh không âm trong trường hợp race condition
                if (newCount == 0) Interlocked.Exchange(ref _activeTourists, 0);

                await Clients.Group("Admins").SendAsync("UpdateActiveUsers", newCount);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
