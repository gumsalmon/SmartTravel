using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using BCrypt.Net;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

        /* ========================================== */
        /* 1. CHỨC NĂNG ĐĂNG NHẬP */
        /* ========================================== */
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });

            bool isPasswordValid = false;
            if (user.PasswordHash == req.Password) { isPasswordValid = true; }
            else
            {
                try { isPasswordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash); } catch { }
            }

            if (!isPasswordValid) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });

            return Ok(new { userId = user.Id, role = user.Role, fullName = user.FullName, username = user.Username });
        }

        /* ========================================== */
        /* 2. CHỨC NĂNG ĐỔI MẬT KHẨU (Đã fix dùng UserId) */
        /* ========================================== */
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            // 💡 TÌM TRỰC TIẾP BẰNG ID ĐỂ KHÔNG BAO GIỜ BỊ TRƯỢT
            var user = await _context.Users.FindAsync(req.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            // Kiểm tra mật khẩu cũ
            bool isOldValid = false;
            if (user.PasswordHash == req.OldPassword) { isOldValid = true; }
            else
            {
                try { isOldValid = BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash); } catch { }
            }

            if (!isOldValid) return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });

            // Mã hóa và lưu mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
    }

    /* --- DTOs (Data Transfer Objects) --- */
    public class LoginRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }

    // 💡 ĐÃ FIX: Đổi Username thành UserId (int) để chính xác 100%
    public class ChangePasswordRequest { public int UserId { get; set; } public string OldPassword { get; set; } = ""; public string NewPassword { get; set; } = ""; }
}