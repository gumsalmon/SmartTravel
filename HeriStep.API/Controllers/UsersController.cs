using HeriStep.Shared.Models.DTOs.Responses;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly HeriStepDbContext _context;

        public UsersController(HeriStepDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. LẤY DANH SÁCH TỔNG HỢP (Kèm số sạp - Trang Index)
        // ==========================================
        [HttpGet("owners-summary")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetOwnersSummary()
        {
            var summary = await _context.Users
                .Where(u => u.Role == "StallOwner" && !u.IsDeleted) // 💡 Chỉ lấy tài khoản chưa bị xóa mềm
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName ?? "Chưa đặt tên",
                    Role = u.Role,
                    // Đếm số sạp mà chủ này đang quản lý và sạp đó chưa bị xóa
                    StallCount = _context.Stalls.Count(s => s.OwnerId == u.Id && !s.IsDeleted)
                })
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return Ok(summary);
        }

        // ==========================================
        // 2. LẤY DANH SÁCH CHO DROPDOWN
        // ==========================================
        [HttpGet("owners")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetOwners()
        {
            return await _context.Users
                .Where(u => u.Role == "StallOwner" && !u.IsDeleted)
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
            // Kiểm tra trùng Số điện thoại (Username)
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Số điện thoại này đã từng được cấp tài khoản trên hệ thống!");

            // Xử lý mật khẩu mặc định và Hash bảo mật
            string passwordToHash = string.IsNullOrEmpty(dto.Password) ? "123456" : dto.Password;

            var newUser = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash),
                FullName = dto.FullName,
                Role = "StallOwner",
                IsDeleted = false,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cấp tài khoản thành công!" });
        }

        // ==========================================
        // 4. RESET MẬT KHẨU VỀ 123456 (Dùng POST để khớp IndexModel)
        // ==========================================
        [HttpPost("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
            user.UpdatedAt = DateTime.Now;

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

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (existingUser == null) return NotFound();

            existingUser.FullName = dto.FullName;
            existingUser.UpdatedAt = DateTime.Now;

            // Nếu Admin có nhập mật khẩu mới thì mới Hash lại
            if (!string.IsNullOrEmpty(dto.Password))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==========================================
        // 6. XÓA TÀI KHOẢN (SOFT DELETE)
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null) return NotFound();

            // 1. Thực hiện xóa mềm User
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.Now;

            // 2. Gỡ bỏ liên kết: Cho các sạp thuộc user này thành "Vô chủ"
            var relatedStalls = await _context.Stalls.Where(s => s.OwnerId == id).ToListAsync();
            foreach (var s in relatedStalls)
            {
                s.OwnerId = null;
                s.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa tài khoản và gỡ liên kết sạp thành công." });
        }
    }
}