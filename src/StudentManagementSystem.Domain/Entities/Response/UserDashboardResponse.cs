namespace StudentManagementSystem.Domain.Entities.Response;

public class UserDashboardResponse
{
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalParents { get; set; }
}