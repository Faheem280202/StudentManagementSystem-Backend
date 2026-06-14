using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Application.Interfaces;

namespace StudentManagementSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;

    public RolesController(IRoleRepository roleRepository) => _roleRepository = roleRepository;

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleRepository.GetAllRolesAsync();

        return Ok(roles);
    }
}
