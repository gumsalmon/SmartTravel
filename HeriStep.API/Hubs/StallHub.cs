using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HeriStep.API.Hubs
{
    // Đây là "Trạm thu phát sóng" để API bắn tín hiệu sang Web
    public class StallHub : Hub
    {
        // Khách hàng vào sạp -> gọi hàm này -> bắn sự kiện về cho Web Dashboard
        public async Task TouristArrived(int stallId, string langCode)
        {
            // Gửi sự kiện "OnTouristArrived" tới tất cả các Web đang mở
            await Clients.All.SendAsync("OnTouristArrived", stallId, langCode);
        }
    }
}