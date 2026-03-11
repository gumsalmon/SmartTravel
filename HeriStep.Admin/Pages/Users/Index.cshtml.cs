using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        // Danh sách hiển thị trên bảng
        public List<UserDto> UserList { get; set; } = new();

        [BindProperty] public UserDto NewUser { get; set; } = new();

        // ==========================================
        // 1. LẤY DANH SÁCH (GET)
        // ==========================================
        public async Task OnGetAsync()
        {
            try
            {
                // Gọi endpoint summary để lấy đầy đủ tên và số lượng sạp
                UserList = await _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners-summary") ?? new();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Không thể kết nối tới server API: " + ex.Message;
            }
        }

        // ==========================================
        // 2. TẠO TÀI KHOẢN (POST)
        // ==========================================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // Gán mật khẩu mặc định nếu Admin để trống [cite: 2026-01-14]
            if (string.IsNullOrEmpty(NewUser.Password)) NewUser.Password = "123456";

            try
            {
                var response = await _http.PostAsJsonAsync("api/Users", NewUser);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"✅ Đã cấp tài khoản cho {NewUser.FullName} thành công!";
                    return RedirectToPage();
                }

                // Đọc lỗi chi tiết từ API (ví dụ: "Số điện thoại đã tồn tại")
                var errorMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "❌ " + (string.IsNullOrEmpty(errorMsg) ? "Lỗi khi tạo tài khoản!" : errorMsg);
            }
            catch
            {
                TempData["Error"] = "❌ Lỗi hệ thống khi gọi API tạo người dùng.";
            }

            await OnGetAsync();
            return Page();
        }

        // ==========================================
        // 3. RESET MẬT KHẨU (Bổ sung cho giao diện của bạn)
        // ==========================================
        public async Task<IActionResult> OnPostResetPasswordAsync(int id)
        {
            try
            {
                // Gọi API reset mật khẩu về mặc định 123456
                var response = await _http.PostAsync($"api/Users/reset-password/{id}", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "✅ Mật khẩu đã được đưa về mặc định (123456).";
                }
                else
                {
                    TempData["Error"] = "❌ Không thể reset mật khẩu cho tài khoản này.";
                }
            }
            catch
            {
                TempData["Error"] = "❌ Lỗi kết nối khi reset mật khẩu.";
            }

            return RedirectToPage();
        }

        // ==========================================
        // 4. XÓA TÀI KHOẢN (DELETE)
        // ==========================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Users/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "🗑️ Đã xóa tài khoản chủ sạp thành công!";
                }
                else
                {
                    TempData["Error"] = "❌ Lỗi: Không thể xóa tài khoản này (Có thể đang ràng buộc dữ liệu).";
                }
            }
            catch
            {
                TempData["Error"] = "❌ Lỗi kết nối khi thực hiện xóa.";
            }

            return RedirectToPage();
        }
    }
}