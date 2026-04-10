using HeriStep.Shared.Models.DTOs.Requests;
using HeriStep.Shared.Models.DTOs.Responses;
using BCrypt.Net;
using HeriStep.API.Data;
using HeriStep.API.Interfaces;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        private readonly IAuthService _authService;

        public AuthController(HeriStepDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        /* ========================================== */
        /* 1. CHỨC NĂNG ĐĂNG NHẬP (KHỚP UML 100%)     */
        /* ========================================== */
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var token = await _authService.LoginAsync(req);

            if (token == null)
            {
                return Unauthorized(new { message = "Sai tài khoản, mật khẩu hoặc tài khoản đã bị khóa!" });
            }

            // 💡 TECH LEAD FIX: Lấy thêm thông tin User từ DB để trả về kèm Token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == req.Username && !u.IsDeleted);

            // Gửi đầy đủ ID, Tên, Quyền về cho Web Chủ Sạp để nó không bị "mù" danh tính nữa
            return Ok(new
            {
                token = token,
                userId = user?.Id ?? 0,
                fullName = user?.FullName ?? "Chủ sạp",
                role = user?.Role ?? "StallOwner",
                message = "Đăng nhập thành công!"
            });
        }


        /* ========================================== */
        /* 2. CHỨC NĂNG ĐỔI MẬT KHẨU                  */
        /* ========================================== */
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && !u.IsDeleted);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            bool isOldValid = false;
            if (user.PasswordHash == req.OldPassword) { isOldValid = true; }
            else
            {
                try { isOldValid = BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash); } catch { }
            }

            if (!isOldValid) return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }

        /* ========================================== */
        /* 3. CHỨC NĂNG ĐĂNG XUẤT (KHỚP UML 100%)     */
        /* ========================================== */
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                await _authService.LogoutAsync(token);
            }

            return Ok(new { message = "Đã thu hồi phiên làm việc trên Server." });
        }
    }

    // 💡 TRẢ LẠI CLASS CHANGE PASSWORD ĐỂ C# KHÔNG KÊU CA NỮA
    public class ChangePasswordRequest
    {
        public int UserId { get; set; }
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}