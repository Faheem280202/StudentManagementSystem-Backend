using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Domain.Entities;
using System.Security.Claims;

namespace StudentManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController(
    IUserRepository userRepository,
    IPasswordService passwordService) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        var dashboard = await _userRepository.GetDashboardAsync(currentUserId, role);

        return Ok(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        var users = role switch
        {
            "Admin" => await _userRepository.GetAdminUsersAsync(),

            "Teacher" => await _userRepository.GetTeacherUsersAsync(currentUserId),

            "Student" => await _userRepository.GetStudentParentsAsync(currentUserId),

            "Parent" => await _userRepository.GetParentChildrenAsync(currentUserId),

            _ => []
        };

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var currentUserId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        var canAccess = await _userRepository.CanAccessUserAsync(currentUserId, role, id);

        if (!canAccess)
        {
            return Forbid();
        }

        var user = await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateUser(User user)
    {
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (currentRole == "Teacher")
        {
            if (user.RoleId != 3 && user.RoleId != 4)
                return BadRequest("Teachers can only create Students and Parents.");
        }

        user.CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
        user.PasswordHash = _passwordService.HashPassword(user.PasswordHash);
        var id = await _userRepository.CreateUserAsync(user);
        var createdUser = await _userRepository.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetUser), new { id }, createdUser);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers()
    {
        var teachers = await _userRepository.GetTeachersAsync();

        return Ok(teachers);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost("student")]
    public async Task<IActionResult> CreateStudent(
        CreateStudentRequest request)
    {
        var currentUserId = Convert.ToInt32(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        var currentRole =
            User.FindFirst(ClaimTypes.Role)?.Value;

        var createdBy =
            User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        if (currentRole == "Teacher")
        {
            request.TeacherId = currentUserId;
        }

        request.StudentPassword =
            _passwordService.HashPassword(
                request.StudentPassword);

        request.ParentPassword =
            _passwordService.HashPassword(
                request.ParentPassword);

        var studentId =
            await _userRepository.CreateStudentAsync(
                request,
                createdBy);

        return Ok(new
        {
            Message = "Student created successfully",
            StudentId = studentId
        });
    }
}