using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Tours
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<Tour> Tours { get; set; } = new();

        [BindProperty] public Tour NewTour { get; set; } = new();
        [BindProperty] public Tour EditTour { get; set; } = new();

        // ==========================================
        // 1. LẤY DANH SÁCH LỘ TRÌNH TỪ API
        // ==========================================
        public async Task OnGetAsync()
        {
            try
            {
                // Sếp nhớ đảm bảo URL API khớp với port của project API đang chạy nhé
                Tours = await _http.GetFromJsonAsync<List<Tour>>("http://127.0.0.1:5297/api/Tours") ?? new();
            }
            catch
            {
                TempData["Error"] = "❌ Không thể kết nối tới Server API.";
            }
        }

        // ==========================================
        // 2. XỬ LÝ THÊM MỚI LỘ TRÌNH
        // ==========================================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrEmpty(NewTour.TourName))
            {
                TempData["Error"] = "❌ Vui lòng nhập tên lộ trình!";
                return RedirectToPage();
            }

            try
            {
                NewTour.Id = 0;
                NewTour.IsActive = true; // Mặc định khi tạo mới là hoạt động

                var response = await _http.PostAsJsonAsync("http://127.0.0.1:5297/api/Tours", NewTour);
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = $"🎉 Đã tạo thành công lộ trình: {NewTour.TourName}";
                else
                    TempData["Error"] = "❌ Lỗi hệ thống khi lưu dữ liệu.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối API."; }

            return RedirectToPage();
        }

        // ==========================================
        // 3. XỬ LÝ CẬP NHẬT LỘ TRÌNH (CÓ CẢ IMAGE_URL)
        // ==========================================
        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"http://127.0.0.1:5297/api/Tours/{EditTour.Id}", EditTour);
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "✅ Đã cập nhật thông tin lộ trình!";
                else
                    TempData["Error"] = "❌ Cập nhật thất bại.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối API."; }

            return RedirectToPage();
        }
    }
}