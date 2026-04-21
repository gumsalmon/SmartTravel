using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages.Tours
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<Tour> Tours { get; set; } = new();

        [BindProperty] public Tour NewTour  { get; set; } = new();
        [BindProperty] public Tour EditTour { get; set; } = new();

        // ==========================================
        // 1. LẤY DANH SÁCH LỘ TRÌNH TỪ API
        // ==========================================
        public async Task OnGetAsync()
        {
            try
            {
                Tours = await _http.GetFromJsonAsync<List<Tour>>("http://127.0.0.1:5297/api/Tours") ?? new();
            }
            catch
            {
                TempData["Error"] = "❌ Không thể kết nối tới Server API.";
            }
        }

        // ==========================================
        // 2. TẠO MỚI LỘ TRÌNH
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
                NewTour.Id       = 0;
                NewTour.IsActive = true;

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
        // 3. CẬP NHẬT LỘ TRÌNH
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

        // ==========================================
        // 4. XÓA LỘ TRÌNH
        // ==========================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"http://127.0.0.1:5297/api/Tours/{id}");
                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "🗑️ Đã xóa lộ trình thành công!";
                else
                    TempData["Error"] = "❌ Không thể xóa lộ trình này.";
            }
            catch { TempData["Error"] = "❌ Lỗi kết nối API."; }

            return RedirectToPage();
        }

        // ==========================================
        // 5. TẠO TOUR TRENDING (Gọi /api/Tours/create-trending)
        // ==========================================
        public async Task<IActionResult> OnPostCreateTrendingAsync([FromQuery] int topX = 5)
        {
            try
            {
                var response = await _http.PostAsync(
                    $"http://127.0.0.1:5297/api/Tours/create-trending?topX={topX}", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TrendingResult>();
                    TempData["Success"] = $"🔥 Đã tạo Tour Trending: '{result?.TourName}' với {result?.StallCount} sạp!";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"❌ {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi kết nối: {ex.Message}";
            }

            return RedirectToPage();
        }

        // DTO nội bộ để parse response từ create-trending
        private class TrendingResult
        {
            public string? TourName   { get; set; }
            public int     StallCount { get; set; }
        }
    }
}