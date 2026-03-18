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

        // Danh sách hiển thị trên trang hiện tại
        public List<UserDto> UserList { get; set; } = new();

        [BindProperty] public UserDto NewUser { get; set; } = new();

        // 💡 Thuộc tính phân trang
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;

        public async Task OnGetAsync()
        {
            try
            {
                // 1. Lấy toàn bộ danh sách từ API
                var allUsers = await _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners-summary") ?? new();

                // 2. Cấu hình phân trang (VD: 10 chủ sạp mỗi trang)
                int pageSize = 10;
                int totalItems = allUsers.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Khống chế số trang hợp lệ
                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

                // 3. Cắt danh sách lấy đúng trang hiện tại và sắp xếp theo ID mới nhất
                UserList = allUsers
                    .OrderByDescending(u => u.Id)
                    .Skip((PageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Lỗi kết nối API: " + ex.Message;
                UserList = new();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            if (string.IsNullOrEmpty(NewUser.Password)) NewUser.Password = "123456";

            try
            {
                var response = await _http.PostAsJsonAsync("api/Users", NewUser);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"✅ Đã cấp tài khoản cho {NewUser.FullName} thành công!";
                    return RedirectToPage();
                }
                TempData["Error"] = "❌ " + await response.Content.ReadAsStringAsync();
            }
            catch { TempData["Error"] = "❌ Lỗi hệ thống khi gọi API."; }

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(int id)
        {
            var response = await _http.PostAsync($"api/Users/reset-password/{id}", null);
            if (response.IsSuccessStatusCode) TempData["Success"] = "✅ Mật khẩu đã reset về 123456.";
            else TempData["Error"] = "❌ Không thể reset mật khẩu.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/Users/{id}");
            if (response.IsSuccessStatusCode) TempData["Success"] = "🗑️ Đã xóa tài khoản chủ sạp.";
            else TempData["Error"] = "❌ Lỗi: Tài khoản này có thể đang quản lý sạp nên không thể xóa!";
            return RedirectToPage();
        }
    }
}