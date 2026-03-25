using HeriStep.Shared.Models; // 💡 DÒNG SINH TỬ ĐỂ GỌI ĐƯỢC CLASS STALL
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
                // 2. Gọi API lấy toàn bộ sạp
                var response = await _http.GetAsync($"api/Stalls/admin-map");

                if (response.IsSuccessStatusCode)
                {
                    // 💡 Bật chế độ không phân biệt chữ Hoa - chữ Thường để biến IsExpired không bị nuốt mất
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Chuyển JSON thành danh sách Stall
                    var allStalls = await response.Content.ReadFromJsonAsync<List<Stall>>(options);

                    // Lọc ra những sạp thuộc về User này
                    MyStalls = allStalls?
                        .Where(s => s.OwnerId.ToString() == CurrentUserId)
                        .ToList() ?? new List<Stall>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi trang chủ]: {ex.Message}");
                // Nếu lỗi API thì trả về danh sách trống, tránh sập web
                MyStalls = new List<Stall>();
            }

            return Page();
        }
    }
}