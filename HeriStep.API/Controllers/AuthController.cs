using BCrypt.Net;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public AuthController(HeriStepDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CHỨC NĂNG ĐĂNG KÝ (Dành cho Admin cấp tài khoản)
        // ==========================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (await _context.Users.AnyAsync(u => u.Username == req.Username))
                return BadRequest("Số điện thoại này đã được cấp tài khoản rồi!");

            // 💡 Băm mật khẩu bằng BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var newUser = new User
            {
                Username = req.Username,
                PasswordHash = hashedPassword,
                FullName = req.FullName,
                Role = req.Role ?? "StallOwner"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tạo tài khoản thành công!", username = newUser.Username });
        }

        // ==========================================
        // 2. CHỨC NĂNG ĐĂNG NHẬP
        // ==========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
            }

            bool isPasswordValid = false;

            // Kiểm tra trường hợp đặc biệt: Mật khẩu chưa mã hóa (nếu sếp lỡ tay nhập trực tiếp vào SQL)
            if (user.PasswordHash == req.Password)
            {
                isPasswordValid = true;
            }
            else
            {
                // 💡 Giải mã và so sánh bằng BCrypt
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
                }
                catch { } // Tránh sập server nếu chuỗi hash trong DB bị lỗi định dạng
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
            }

            return Ok(new
            {
                userId = user.Id,
                role = user.Role,
                fullName = user.FullName,
                username = user.Username
            });
        }
    }

    // --- DTOs (Data Transfer Objects) ---
    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Role { get; set; }
    }
}