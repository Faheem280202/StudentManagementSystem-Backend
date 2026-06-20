using StudentManagementSystem.Domain.Common;

namespace StudentManagementSystem.Domain.Entities
{
    public class CreateStudentRequest
    {
        // Student
        public string StudentUserName { get; set; } = string.Empty;
        public string StudentFirstName { get; set; } = string.Empty;
        public string StudentLastName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentPhone { get; set; } = string.Empty;
        public string StudentPassword { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public int TeacherId { get; set; }

        // Parent
        public string ParentUserName { get; set; } = string.Empty;
        public string ParentFirstName { get; set; } = string.Empty;
        public string ParentLastName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string ParentPassword { get; set; } = string.Empty;
    }
}
