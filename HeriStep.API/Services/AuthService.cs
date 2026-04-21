using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using HeriStep.API.Interfaces;
using HeriStep.API.Data;
using HeriStep.Shared.Models;
using HeriStep.Shared.Models.DTOs.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory; // 💡 Khai báo để dùng Cache giả lập Redis

namespace HeriStep.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly HeriStepDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache; // 💡 Dùng làm Blacklist

        public AuthService(HeriStepDbContext context, IConfiguration configuration, IMemoryCache cache)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<string> LoginAsync(LoginRequest request)
        {
            // Bước 1: Query DB kiểm tra Username (Đúng sơ đồ UML)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted);
            if (user == null) return null;

            // Bước 2: Xác thực mật khẩu (Đúng sơ đồ UML)
            bool isPasswordValid = false;
            if (user.PasswordHash == request.Password)
            {
                isPasswordValid = true;
            }
            else
            {
                try { isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash); } catch { }
            }

            if (!isPasswordValid) return null;

            // Bước 3: Tạo JWT Token (Đúng sơ đồ UML)
            return GenerateJwtToken(user);
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return false;

            var newUser = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Role = request.Role ?? "StallOwner",
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LogoutAsync(string token)
        {
            // 💡 Đưa Token vào Blacklist (Vô hiệu hóa Token) - Đúng sơ đồ UML
            // Set thời gian nằm trong Blacklist bằng đúng thời gian sống của Token (2 tiếng)
            _cache.Set($"blacklist_{token}", true, TimeSpan.FromHours(2));

            await Task.CompletedTask;
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "superSecretKey_NeedToChange_InProduction_123456789");
            var tokenHandler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role ?? "StallOwner")
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(descriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}