using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HeriStep.Admin.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty] public string Username { get; set; } = "";
        [BindProperty] public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            // Nếu đã đăng nhập rồi thì đá thẳng vào trang chủ
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToPage("/Index");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // TẠM THỜI HARDCODE ADMIN ĐỂ TEST (Sau này sẽ gọi DB kiểm tra bảng Users)
            if (Username == "admin" && Password == "123456")
            {
                // Tạo giấy thông hành (Claims) cho người này
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Lưu vào Cookie của trình duyệt
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToPage("/Index");
            }

            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return Page();
        }
    }
}