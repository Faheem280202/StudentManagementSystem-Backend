using StudentManagementSystem.Application.DTOs.Auth;

namespace StudentManagementSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
    }
}
