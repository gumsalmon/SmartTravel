using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Net.Http.Json; // Thêm dòng này
using System.Collections.Generic; // Thêm dòng này

namespace HeriStep.Merchant.Pages;

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
            var response = await _http.PostAsJsonAsync("api/Auth/Login", new { Username, Password });

            if (response.IsSuccessStatusCode)
            {
                // Thêm Options để không lo lỗi hoa thường
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<LoginRes>(options);

                if (result != null)
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.userId.ToString()),
                    new Claim(ClaimTypes.Name, result.fullName ?? Username),
                    new Claim(ClaimTypes.Role, result.role ?? "StallOwner")
                };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToPage("/Index");
                }
            }

            // Đọc lỗi: Thay dynamic bằng JsonElement để an toàn hơn
            var errorContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(errorContent))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var msgElement))
                {
                    ErrorMessage = msgElement.GetString();
                }
            }
            ErrorMessage ??= "Tên đăng nhập hoặc mật khẩu không đúng.";
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể kết nối đến máy chủ API (Hãy kiểm tra API đã chạy chưa?).";
        }

        return Page();
    }
}

public class LoginRes
{
    public int userId { get; set; }
    public string? role { get; set; }
    public string? fullName { get; set; }
}