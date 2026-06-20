using StudentManagementSystem.Domain.Entities;
using StudentManagementSystem.Domain.Entities.Response;


namespace StudentManagementSystem.Application.Interfaces
{
    public interface IUserRepository
    {
    Task<UserDashboardResponse> GetDashboardAsync(int currentUserId, string role);
    Task<List<UserResponse>> GetAdminUsersAsync();
    Task<List<UserResponse>> GetTeacherUsersAsync(int teacherId);
    Task<List<UserResponse>> GetStudentParentsAsync(int studentId);
    Task<List<UserResponse>> GetParentChildrenAsync(int parentId);
    Task<UserResponse?> GetByIdAsync(int id);
    Task<int> CreateUserAsync(User user);
    Task<bool> CanAccessUserAsync(
        int currentUserId,
        string role,
        int targetUserId);
    Task<int> CreateStudentAsync(
    CreateStudentRequest request,
    string createdBy);

    Task<List<UserResponse>> GetTeachersAsync();
    }
}