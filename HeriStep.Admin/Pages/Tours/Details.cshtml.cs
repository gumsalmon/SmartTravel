using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Tours
{
    public class DetailsModel : PageModel
    {
        private readonly HttpClient _http;

        public DetailsModel(HttpClient http)
        {
            _http = http;
        }

        [BindProperty]
        public Tour Tour { get; set; } = new();

        public List<Stall> AvailableStalls { get; set; } = new();

        [BindProperty]
        public int SelectedStallId { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // 1. Lấy thông tin Lộ trình (💡 ĐÃ SỬA THÊM DOMAIN API VÀO ĐÂY)
            try
            {
                var data = await _http.GetFromJsonAsync<Tour>($"http://127.0.0.1:5297/api/Tours/{id}");

                if (data == null)
                {
                    TempData["Error"] = "❌ Không tìm thấy lộ trình này trong cơ sở dữ liệu.";
                    return RedirectToPage("./Index");
                }

                Tour = data;
            }
            catch (Exception)
            {
                TempData["Error"] = "❌ Lỗi kết nối đến máy chủ API khi lấy chi tiết lộ trình.";
                return RedirectToPage("./Index");
            }

            // 2. Gọi API lấy danh sách Sạp tự do đổ vào Modal (💡 ĐÃ SỬA VÀO ĐÂY NỮA)
            try
            {
                AvailableStalls = await _http.GetFromJsonAsync<List<Stall>>("http://127.0.0.1:5297/api/Tours/available-stalls") ?? new();
            }
            catch
            {
                AvailableStalls = new List<Stall>();
            }

            return Page();
        }

        // HÀM 1: Thêm sạp có sẵn vào lộ trình
        public async Task<IActionResult> OnPostAddStallAsync(int tourId)
        {
            if (SelectedStallId <= 0) return RedirectToPage(new { id = tourId });

            try
            {
                var response = await _http.PutAsync($"http://127.0.0.1:5297/api/Tours/{tourId}/AddStall/{SelectedStallId}", null);
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "✅ Đã thêm sạp vào lộ trình thành công!";
                else
                    TempData["Error"] = "❌ Không thể thêm sạp. Vui lòng kiểm tra lại.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối đến Server."; }

            return RedirectToPage(new { id = tourId });
        }

        // HÀM 2: Thay đổi thứ tự Lên/Xuống
        public async Task<IActionResult> OnPostMoveStallAsync(int tourId, int stallId, string direction)
        {
            try
            {
                var response = await _http.PutAsync($"http://127.0.0.1:5297/api/Tours/{tourId}/MoveStall/{stallId}?direction={direction}", null);
                if (response.IsSuccessStatusCode) TempData["Success"] = "↕️ Đã cập nhật thứ tự thành công!";
                else TempData["Error"] = "❌ Không thể thay đổi thứ tự. Dữ liệu có thể đang lỗi.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối đến Server."; }

            return RedirectToPage(new { id = tourId });
        }

        // HÀM 3: Gỡ sạp khỏi lộ trình
        public async Task<IActionResult> OnPostRemoveStallAsync(int tourId, int stallId)
        {
            try
            {
                var response = await _http.PutAsync($"http://127.0.0.1:5297/api/Tours/{tourId}/RemoveStall/{stallId}", null);
                if (response.IsSuccessStatusCode) TempData["Success"] = "✅ Đã gỡ quán khỏi lộ trình thành công!";
                else TempData["Error"] = "❌ Không thể gỡ quán này. Có thể quán đã bị gỡ từ trước hoặc không tồn tại.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối đến máy chủ API khi gỡ sạp."; }

            return RedirectToPage(new { id = tourId });
        }
    }
}