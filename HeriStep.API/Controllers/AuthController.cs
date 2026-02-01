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
        private readonly HeriStepDbContext _context; // Đảm bảo tên này khớp với DataContext của bạn

        public AuthController(HeriStepDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CHỨC NĂNG ĐĂNG KÝ (TỰ ĐỘNG MÃ HÓA)
        // ==========================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            // Kiểm tra xem Username đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == req.Username))
                return BadRequest("Tên đăng nhập đã tồn tại!");

            // ĐÂY LÀ CHỖ "TỰ ĐỘNG": Admin nhập 123456, code sẽ băm nó
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var newUser = new User // Giả sử model của bạn tên là User
            {
                Username = req.Username,
                PasswordHash = hashedPassword, // Lưu bản băm vào DB
                FullName = req.FullName,
                Role = req.Role ?? "StallOwner",
                StallId = req.StallId
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("Tạo tài khoản thành công, mật khẩu đã được mã hóa!");
        }

        // ==========================================
        // 2. CHỨC NĂNG ĐĂNG NHẬP
        // ==========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == req.Username);

            // Verify sẽ so khớp Password (chữ thường) với PasswordHash (chuỗi băm)
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
            }

            return Ok(new
            {
                stallID = user.StallId,
                role = user.Role,
                fullName = user.FullName
            });
        }
    }

    // Các Class DTO để nhận dữ liệu từ Web gửi lên
    public class LoginRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? Role { get; set; }
        public int? StallId { get; set; }
    }
}