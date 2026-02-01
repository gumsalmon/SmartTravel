using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HeriStep.Shared; // Để dùng class User

namespace HeriStep.Admin.Pages.Users
{
    public class CreateUserModel : PageModel // Tên class phải khớp với lỗi
    {
        private readonly HttpClient _http;
        public CreateUserModel(HttpClient http) => _http = http;

        [BindProperty]
        public User NewUser { get; set; } = new();

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // Gán mật khẩu thô vào bản ghi, API sẽ tự băm BCrypt
            NewUser.PasswordHash = Password;

            var response = await _http.PostAsJsonAsync("api/Users", NewUser);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }
    }
}