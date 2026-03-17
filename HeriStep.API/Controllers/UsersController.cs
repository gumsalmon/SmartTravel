using BCrypt.Net;
using HeriStep.API.Data;
using HeriStep.Shared;
using HeriStep.Shared.Models; // Đảm bảo namespace này khớp với file UserDto của bạn
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public UsersController(HeriStepDbContext context) => _context = context;

        // ==========================================
        // 1. LẤY DANH SÁCH TỔNG HỢP (Kèm số sạp - Dùng cho trang Index)
        // ==========================================
        [HttpGet("owners-summary")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetOwnersSummary()
        {
            var summary = await _context.Users
                .Where(u => u.Role == "StallOwner")
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName ?? "Chưa đặt tên",
                    Role = u.Role,
                    // Đếm số sạp thuộc về User này trong bảng Stalls
                    StallCount = _context.Stalls.Count(s => s.OwnerId == u.Id)
                })
                .ToListAsync();

            return Ok(summary);
        }

        // ==========================================
        // 2. LẤY DANH SÁCH CHỦ SẠP (Dùng cho Dropdown chọn chủ sạp)
        // ==========================================
        [HttpGet("owners")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetOwners()
        {
            return await _context.Users
                .Where(u => u.Role == "StallOwner")
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role
                }).ToListAsync();
        }

        // ==========================================
        // 3. TẠO TÀI KHOẢN MỚI
        // ==========================================
        [HttpPost]
        public async Task<ActionResult> PostUser(UserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Số điện thoại này đã được cấp tài khoản!");

            // Tự động băm mật khẩu (Mặc định 123456 nếu để trống)
            string passwordToHash = string.IsNullOrEmpty(dto.Password) ? "123456" : dto.Password;

            var newUser = new User // Entity Model
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash),
                FullName = dto.FullName,
                Role = "StallOwner"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ==========================================
        // 4. RESET MẬT KHẨU VỀ 123456
        // ==========================================
        [HttpPut("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mật khẩu đã được đưa về 123456" });
        }

        // ==========================================
        // 5. CẬP NHẬT THÔNG TIN CƠ BẢN
        // ==========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UserDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            existingUser.FullName = dto.FullName;

            // Chỉ cập nhật mật khẩu nếu Admin nhập mật khẩu mới
            if (!string.IsNullOrEmpty(dto.Password))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { throw; }

            return NoContent();
        }

        // ==========================================
        // 6. XÓA TÀI KHOẢN
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Lưu ý: Nếu có ràng buộc khóa ngoại, hãy đảm bảo xử lý Cascade Delete trong SQL
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}