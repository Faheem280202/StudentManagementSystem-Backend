using System;
using System.Collections.Generic;
using System.Text;

namespace StudentManagementSystem.Application.DTOs.Users
{
    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

