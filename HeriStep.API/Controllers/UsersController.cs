using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeriStep.API.Data;
using HeriStep.Shared;
using BCrypt.Net;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly HeriStepDbContext _context;
        public UsersController(HeriStepDbContext context) => _context = context;

        // 1. LẤY DANH SÁCH TẤT CẢ USER
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // 2. TẠO USER MỚI (Admin dùng để cấp TK cho chủ sạp)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest("Tên đăng nhập đã tồn tại!");

            // Tự động băm mật khẩu trước khi lưu vào SQL
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        // 3. CẬP NHẬT THÔNG TIN USER
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id) return BadRequest();

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            // Cập nhật thông tin cơ bản
            existingUser.FullName = user.FullName;
            existingUser.StallId = user.StallId;
            existingUser.Role = user.Role;

            // Chỉ băm lại mật khẩu nếu Admin nhập mật khẩu mới
            if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash != existingUser.PasswordHash)
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { throw; }

            return NoContent();
        }

        // 4. XÓA USER
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}