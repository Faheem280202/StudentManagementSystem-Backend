using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Domain.Entities;
using StudentManagementSystem.Infrastructure.Repositories;

namespace StudentManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(IUserRepository userRepository, IPasswordService passwordService) : ControllerBase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordService _passwordService = passwordService;

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var roles = await _userRepository.GetAllUsersAsync();

            return Ok(roles);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            user.PasswordHash = _passwordService.HashPassword(user.PasswordHash);

            var id = await _userRepository.CreateUserAsync(user);

            return await GetUser(id);
        }
    }
}

