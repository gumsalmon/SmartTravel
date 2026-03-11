using BCrypt.Net;
using HeriStep.API.Data;
using HeriStep.Shared;
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
            // Kiểm tra xem Username (Số điện thoại) đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == req.Username))
                return BadRequest("Số điện thoại này đã được cấp tài khoản rồi!");

            // Tự động băm mật khẩu để bảo mật
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var newUser = new User
            {
                Username = req.Username,
                PasswordHash = hashedPassword,
                FullName = req.FullName,
                Role = req.Role ?? "StallOwner"
                // ĐÃ XÓA StallId tại đây vì 1 User có thể có nhiều sạp
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

            // Kiểm tra tài khoản và xác thực mật khẩu băm
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
            }

            // Trả về thông tin cơ bản. 
            // Client sẽ dùng userId này để lấy danh sách sạp tương ứng sau.
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
        public string Username { get; set; } = ""; // Số điện thoại
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Role { get; set; }
        // KHÔNG CÓ StallId ở đây nữa
    }
}