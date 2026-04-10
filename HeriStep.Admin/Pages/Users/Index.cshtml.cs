using HeriStep.Shared.Models.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<UserDto> UserList { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
        public int CurrentPage => PageNumber;

        public async Task OnGetAsync()
        {
            try
            {
                var allUsers = await _http.GetFromJsonAsync<List<UserDto>>("api/Users/owners-summary") ?? new();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();
                    allUsers = allUsers.Where(u =>
                        (!string.IsNullOrEmpty(u.Username) && u.Username.ToLower().Contains(term)) ||
                        (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(term))
                    ).ToList();
                }

                int pageSize = 10;
                int totalItems = allUsers.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;

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