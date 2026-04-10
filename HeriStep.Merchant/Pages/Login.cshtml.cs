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
            var response = await _http.PostAsJsonAsync("http://localhost:5297/api/Auth/Login", new { Username, Password });

            if (response.IsSuccessStatusCode)
            {
                // Thêm Options để không lo lỗi hoa thường
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<LoginRes>(options);

                if (result != null)
                {
                    // 💡 ĐỌC TOKEN TỪ API THEO CHUẨN SEQUENCE 1.1 Tạo JWT Token 
                    var jwtToken = result.token ?? result.Token ?? "";

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.userId.ToString()),
                        new Claim(ClaimTypes.Name, result.fullName ?? Username),
                        new Claim(ClaimTypes.Role, result.role ?? "StallOwner"),
                        new Claim("jwt_token", jwtToken) // 💡 LƯU KÈM TOKEN ĐỂ LOGOUT SỬ DỤNG
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // 💡 ĐÚNG UML: "Lưu thông tin phiên đăng nhập" (Vào cookie)
                    var authProperties = new AuthenticationProperties { IsPersistent = true };
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                    return RedirectToPage("/Index");
                }
            }

            // Đọc lỗi: Thay dynamic bằng JsonElement để an toàn hơn
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
                    ErrorMessage = $"Lỗi định dạng trả về: {errorContent}";
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

public class LoginRes
{
    public int userId { get; set; }
    public string? role { get; set; }
    public string? fullName { get; set; }
    public string? token { get; set; }
    public string? Token { get; set; }
}