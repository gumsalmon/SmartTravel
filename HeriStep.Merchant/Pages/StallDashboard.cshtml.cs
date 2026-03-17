using HeriStep.Shared.Models; // Nhớ using Model của bạn
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeriStep.Merchant.Pages
{
    [Authorize]
    public class StallDashboardModel : PageModel
    {
        private readonly HttpClient _http;

        public StallDashboardModel(HttpClient http)
        {
            _http = http;
        }

        public int CurrentStallId { get; set; }
        public PointOfInterest StallInfo { get; set; } = new PointOfInterest();

        public async Task OnGetAsync(int stallId)
        {
            CurrentStallId = stallId;

            try
            {
                // Gọi API để lấy thông tin chi tiết của sạp (Tên, Ảnh, Giờ mở cửa, Lời chào TTS...)
                // Lưu ý: Nếu API này bạn chưa viết, giao diện vẫn lên nhưng các ô input sẽ trống.
                var data = await _http.GetFromJsonAsync<PointOfInterest>($"api/Stalls/{stallId}");
                if (data != null)
                {
                    StallInfo = data;
                }
            }
            catch
            {
                // Bắt lỗi rớt mạng hoặc API chưa có để web không bị sập
            }
        }
    }
}