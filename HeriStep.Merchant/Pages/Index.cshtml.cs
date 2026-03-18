using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HeriStep.Merchant.Pages
{
    [Authorize] // 💡 Chỉ cho phép người đã đăng nhập mới vào được trang này
    public class IndexModel : PageModel
    {
        private readonly HttpClient _http;

        public IndexModel(HttpClient http)
        {
            _http = http;
        }

        // Danh sách các sạp mà chủ này sở hữu
        public List<StallDto> MyStalls { get; set; } = new();

        // ID của người dùng đang đăng nhập
        public string CurrentUserId { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Lấy UserId từ Cookie Authentication (Claims)
            // ClaimTypes.NameIdentifier thường chứa ID của User
            CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value
                            ?? "";

            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                // 2. Gọi API để lấy danh sách sạp của riêng ông chủ này
                // Giả sử API có endpoint lấy sạp theo OwnerId
                var response = await _http.GetAsync($"api/Stalls/admin-map");

                if (response.IsSuccessStatusCode)
                {
                    var allStalls = await response.Content.ReadFromJsonAsync<List<StallDto>>();

                    // Lọc ra những sạp thuộc về User này
                    MyStalls = allStalls?
                        .Where(s => s.OwnerId.ToString() == CurrentUserId)
                        .ToList() ?? new List<StallDto>();
                }
            }
            catch (Exception)
            {
                // Nếu lỗi API thì trả về danh sách trống, tránh sập web
                MyStalls = new List<StallDto>();
            }

            return Page();
        }
    }

    // Class DTO tạm thời để hứng dữ liệu từ API
    public class StallDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public bool IsOpen { get; set; }
        public int? OwnerId { get; set; }
    }
}