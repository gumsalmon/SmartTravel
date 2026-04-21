using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace HeriStep.Merchant.Pages
{
    [Authorize] // Chỉ cho phép người đã đăng nhập mới vào được trang này
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;

        public IndexModel(HttpClient http)
        {
            _http = http;
        }

        // Danh sách các sạp mà chủ này sở hữu
        public List<Stall> MyStalls { get; set; } = new();

        // ID của người dùng đang đăng nhập
        public string CurrentUserId { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Lấy UserId từ Cookie Authentication (Claims)
            CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value
                            ?? "";

            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                // 💡 BÍ KÍP Ở ĐÂY: Móc JWT Token từ túi ra và nhét vào Header (Đưa thẻ cho bảo vệ xem)
                var jwtToken = User.FindFirst("jwt_token")?.Value;
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
                }

                // Nhớ check lại cổng 5297 xem có đúng với cổng API đang chạy không nhé sếp
                var apiUrl = $"http://localhost:5297/api/Stalls/my-stalls?ownerId={CurrentUserId}";

                var response = await _http.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    MyStalls = await response.Content.ReadFromJsonAsync<List<Stall>>(options) ?? new List<Stall>();
                }
                else
                {
                    // Nếu vẫn lỗi thì lôi cổ nó ra màn hình xem tiếp
                    TempData["ErrorMessage"] = $"Lỗi API: {response.StatusCode} - Có thể API báo lỗi bên trong.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                MyStalls = new List<Stall>();
            }

            return Page();
        }
    }
}