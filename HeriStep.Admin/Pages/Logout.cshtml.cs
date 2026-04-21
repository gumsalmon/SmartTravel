using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HeriStep.Admin.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly HttpClient _http;

        public LogoutModel(HttpClient http)
        {
            _http = http;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Lấy Token từ Cookie Session
            var token = User.FindFirst("jwt_token")?.Value;

            // 1. NGẮT KẾT NỐI WEBSOCKET NẾU ĐANG CÓ (Làm ở phần Frontend HTML/JS)

            // 2. GỌI API BACKEND ĐỂ THU HỒI TOKEN (Đưa vào Blacklist)
            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    // 💡 ĐÚNG CHUẨN UML 1.2: POST /api/Auth/Logout (Gửi kèm Access Token)
                    _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                await _http.PostAsync("http://localhost:5297/api/Auth/Logout", null);
            }
            catch (Exception)
            {
                // Bỏ qua nếu mất mạng
            }

            // 3. THU HỒI THẺ (Xóa Cookie đăng nhập của server)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 4. Trả về Page để file HTML chạy Javascript xóa LocalStorage/Websocket rồi mới chuyển trang
            return Page();
        }
    }
}