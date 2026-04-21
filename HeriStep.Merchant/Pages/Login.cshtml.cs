using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Net.Http.Json;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace HeriStep.Merchant.Pages
{
    public class LoginModel : PageModel
    {
        private readonly HttpClient _http;
        public LoginModel(HttpClient http) => _http = http;

        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var response = await _http.PostAsJsonAsync("http://localhost:5297/api/Auth/Login", new { Username, Password });

                if (response.IsSuccessStatusCode)
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await response.Content.ReadFromJsonAsync<LoginRes>(options);

                    if (result != null)
                    {
                        // 💡 ĐÃ SỬA LỖI: Chỉ dùng đúng biến "Token" viết hoa, không gọi "token" viết thường nữa!
                        var jwtToken = result.Token ?? "";

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, result.userId.ToString()),
                            new Claim(ClaimTypes.Name, result.fullName ?? Username),
                            new Claim(ClaimTypes.Role, result.role ?? "StallOwner"),
                            new Claim("jwt_token", jwtToken)
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        var authProperties = new AuthenticationProperties { IsPersistent = true };
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                        return RedirectToPage("/Index");
                    }
                }

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
                    }
                    catch
                    {
                        ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    }
                }
                ErrorMessage ??= "Tên đăng nhập hoặc mật khẩu không đúng.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể kết nối đến máy chủ API (Lỗi: {ex.Message}).";
            }

            return Page();
        }
    }

    // 💡 ĐÃ DỌN SẠCH: Chỉ giữ lại đúng 1 chữ Token viết hoa để không đụng độ JSON nữa
    public class LoginRes
    {
        public int userId { get; set; }
        public string? role { get; set; }
        public string? fullName { get; set; }
        public string? Token { get; set; }
    }
}