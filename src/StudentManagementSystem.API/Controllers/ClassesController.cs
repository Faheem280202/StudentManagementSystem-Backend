using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Application.Interfaces;

namespace StudentManagementSystem.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClassesController(IClassRepository classRepository) : ControllerBase
{
    private readonly IClassRepository _classRepository = classRepository;

    [HttpGet]
    public async Task<IActionResult> GetClasses()
    {
        var classes = await _classRepository.GetClassesAsync();

        return Ok(classes);
    }
}