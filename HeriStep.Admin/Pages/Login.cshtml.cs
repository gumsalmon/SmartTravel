using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;

namespace HeriStep.Admin.Pages // (Hoặc HeriStep.Merchant.Pages tùy project sếp đang mở)
{
    public class LoginModel : PageModel
    {
        private readonly HttpClient _http;

        public LoginModel(HttpClient http)
        {
            _http = http;
        }

        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        // 💡 ĐÃ SỬA: Đưa về nguyên trạng, không gọi SignOutAsync ở đây nữa để né lỗi 400!
        public IActionResult OnGet()
        {
            // Tắt trình duyệt là mất phiên do đã cài IsPersistent = false ở dưới
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var loginData = new { Username = this.Username, Password = this.Password };

                var response = await _http.PostAsJsonAsync("http://localhost:5297/api/Auth/Login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    // 💡 ĐỌC TOKEN TỪ API THEO CHUẨN SEQUENCE 1.1 "API -> API: Tạo JWT Token"
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<LoginRes>(options);
                    var jwtToken = result?.Token ?? "";

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, Username),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim("jwt_token", jwtToken) // 💡 LƯU KÈM TOKEN ĐỂ LOGOUT SỬ DỤNG
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // 💡 IsPersistent = true
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    // 💡 ĐÚNG UML: "Lưu thông tin phiên đăng nhập" (Vào cookie)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToPage("/Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(errorContent))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                            if (doc.RootElement.TryGetProperty("message", out var msgElement))
                            {
                                ErrorMessage = msgElement.GetString();
                            }
                        } catch { }
                    }
                    ErrorMessage ??= "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    return Page();
                }
            }
            catch (System.Exception)
            {
                ErrorMessage = "Không thể kết nối đến API Backend. (Kiểm tra lại xem API có đang chạy không nhé sếp!)";
                return Page();
            }
        }
    }

    public class LoginRes
    {
        public string? Token { get; set; }
    }
}