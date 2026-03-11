using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Subscriptions
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        // Danh sách thiết bị hiển thị lên bảng
        public List<Subscription> SubList { get; set; } = new();

        // Biến hứng dữ liệu từ Modal Kích hoạt
        [BindProperty]
        public Subscription NewSub { get; set; } = new();

        // ==========================================
        // 1. LẤY DANH SÁCH GÓI CƯỚC (GET)
        // ==========================================
        public async Task OnGetAsync()
        {
            try
            {
                // Gọi API lấy toàn bộ danh sách thiết bị
                SubList = await _http.GetFromJsonAsync<List<Subscription>>("api/Subscriptions") ?? new();

                // Sắp xếp ưu tiên: Máy mới kích hoạt lên đầu, máy hết hạn xuống dưới
                SubList = SubList.OrderByDescending(s => s.IsActive).ThenByDescending(s => s.StartDate).ToList();
            }
            catch
            {
                TempData["Error"] = "❌ Lỗi kết nối API lấy dữ liệu Gói Subscription.";
            }
        }

        // ==========================================
        // 2. XỬ LÝ KÍCH HOẠT THIẾT BỊ MỚI (POST)
        // ==========================================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();

            // Chỉ Validate những trường người dùng nhập (DeviceId, ActivationCode)
            if (!TryValidateModel(NewSub, nameof(NewSub)))
            {
                TempData["Error"] = "❌ Dữ liệu nhập vào không hợp lệ!";
                return RedirectToPage(); // Reset lại trang
            }

            try
            {
                // 💡 LOGIC TỰ ĐỘNG HÓA DÀNH CHO ADMIN:
                NewSub.Id = 0; // Để SQL tự tăng ID
                NewSub.StartDate = DateTime.Now; // Bắt đầu tính từ ngay lúc bấm nút
                NewSub.ExpiryDate = DateTime.Now.AddDays(30); // Tự động cộng 30 ngày sử dụng
                NewSub.IsActive = true; // Bật trạng thái hoạt động ngay lập tức

                // Gửi dữ liệu xuống API để lưu vào Database
                var response = await _http.PostAsJsonAsync("api/Subscriptions", NewSub);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"✅ Đã kích hoạt thành công thiết bị {NewSub.DeviceId} với thời hạn 30 ngày!";
                }
                else
                {
                    TempData["Error"] = "❌ Kích hoạt thất bại: " + await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Lỗi hệ thống: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}