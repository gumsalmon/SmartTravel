using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        // Giả lập gọi API login trả về StallID
        var response = await _http.PostAsJsonAsync("api/Auth/Login", new { Username, Password });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginRes>();
            if (result != null)
            {
                // Lưu StallID vào Session
                HttpContext.Session.SetInt32("MyStallID", result.StallId);
                return RedirectToPage("Index");
            }
        }
        ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
        return Page();
    }
}
public class LoginRes { public int StallId { get; set; } }