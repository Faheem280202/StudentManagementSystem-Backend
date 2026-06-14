using StudentManagementSystem.Domain.Entities;
using StudentManagementSystem.Domain.Entities.Response;

namespace StudentManagementSystem.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<List<UserResponse>> GetAllUsersAsync();
        Task<UserResponse?> GetByIdAsync(int id);
        Task<int> CreateUserAsync(User user);
    }
}

