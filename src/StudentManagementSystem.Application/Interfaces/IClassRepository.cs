using StudentManagementSystem.Domain.Entities.Response;

namespace StudentManagementSystem.Application.Interfaces;

public interface IClassRepository
{
    Task<List<ClassResponse>> GetClassesAsync();
}