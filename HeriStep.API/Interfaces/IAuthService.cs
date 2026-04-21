using System.Threading.Tasks;
using HeriStep.Shared.Models.DTOs.Requests;

namespace HeriStep.API.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);

        // 💡 THÊM MỚI: Khung chuẩn bị cho chức năng Blacklist Token khi Đăng xuất
        Task<bool> LogoutAsync(string token);
    }
}