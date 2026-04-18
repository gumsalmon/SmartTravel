using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace HeriStep.Admin.Hubs
{
    /// <summary>
    /// Hub đếm khách đang dùng App (anonymous users).
    /// Quy tắc: KHÔNG đếm Admin đã đăng nhập đang xem Dashboard.
    /// </summary>
    public class DashboardHub : Hub
    {
        // Counter tĩnh — an toàn với Interlocked (thread-safe)
        private static int _activeTourists = 0;

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                // Là Admin đã đăng nhập → thêm vào group Admins, chỉ nhận thông báo
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");

                // Gửi ngay số hiện tại cho Admin vừa kết nối
                await Clients.Caller.SendAsync("UpdateActiveTourists", _activeTourists);
            }
            else
            {
                // Là App Client (Khách ẩn danh) → tăng đếm
                Interlocked.Increment(ref _activeTourists);

                // Broadcast số mới tới tất cả Admin đang xem Dashboard
                await Clients.Group("Admins").SendAsync("UpdateActiveTourists", _activeTourists);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                // Khách ẩn danh tắt app → giảm đếm (không để âm)
                var current = Interlocked.Decrement(ref _activeTourists);
                if (current < 0) Interlocked.Exchange(ref _activeTourists, 0);

                await Clients.Group("Admins").SendAsync("UpdateActiveTourists", Math.Max(0, current));
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
