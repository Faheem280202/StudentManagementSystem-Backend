using StudentManagementSystem.Application.DTOs.Auth;

namespace StudentManagementSystem.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(LoginResponse user);
    }
}
