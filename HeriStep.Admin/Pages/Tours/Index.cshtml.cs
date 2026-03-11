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

        // Lấy danh sách Lộ trình từ API
        public async Task OnGetAsync()
        {
            try
            {
                Tours = await _http.GetFromJsonAsync<List<Tour>>("api/Tours") ?? new();
            }
            catch
            {
                TempData["Error"] = "❌ Không thể kết nối tới Server API.";
            }
        }

        // Xử lý Thêm mới
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

                var response = await _http.PostAsJsonAsync("api/Tours", NewTour);
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = $"🎉 Đã tạo thành công lộ trình: {NewTour.TourName}";
                else
                    TempData["Error"] = "❌ Lỗi hệ thống khi lưu dữ liệu.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối API."; }

            return RedirectToPage();
        }

        // Xử lý Cập nhật
        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/Tours/{EditTour.Id}", EditTour);
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "✅ Đã cập nhật thông tin lộ trình!";
                else
                    TempData["Error"] = "❌ Cập nhật thất bại.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối API."; }

            return RedirectToPage();
        }

        // Ghi chú: Đã loại bỏ OnPostDeleteAsync theo yêu cầu.
    }
}