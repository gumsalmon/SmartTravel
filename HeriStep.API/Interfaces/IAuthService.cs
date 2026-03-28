using System;
using System.Threading.Tasks;
using HeriStep.Shared.Models;
using HeriStep.Shared.Models.DTOs.Requests;

namespace HeriStep.API.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);
    }
}
