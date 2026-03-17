
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Net.Http.Json;

namespace HeriStep.Merchant.Pages // (Đổi Merchant thành tên project của bạn)
{
    [Authorize] // Bắt buộc phải đăng nhập mới được vào trang này
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;
        public IndexModel(HttpClient http) => _http = http;

        public List<PointOfInterest> MyStalls { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Lấy ID của chủ sạp đang đăng nhập từ Cookie
            var ownerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(ownerIdString))
                return RedirectToPage("/Login"); // Lỗi thì đuổi ra login lại

            // 2. Gọi API lấy danh sách sạp
            try
            {
                MyStalls = await _http.GetFromJsonAsync<List<PointOfInterest>>($"api/Points/owner/{ownerIdString}") ?? new();
            }
            catch
            {
                MyStalls = new();
            }

            return Page();
        }
    }
}