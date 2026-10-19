using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;

    public SkillsController(ISkillService skillService)
    {
        _skillService = skillService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSkillRequest request)
    {
        var teacherId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(teacherId))
            return Unauthorized();

        var skill = await _skillService.CreateAsync(
            request, teacherId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = skill.Id },
            skill);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category)
    {
        var skills = await _skillService.GetAllAsync(
            search, category);

        return Ok(skills);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var skill = await _skillService.GetByIdAsync(id);

        if (skill is null)
            return NotFound(new { message = "Skill not found." });

        return Ok(skill);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateSkillRequest request)
    {
        var teacherId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(teacherId))
            return Unauthorized();

        var skill = await _skillService.UpdateAsync(
            id, request, teacherId);

        if (skill is null)
        {
            return NotFound(new
            {
                message = "Skill not found or you do not own this listing."
            });
        }

        return Ok(skill);
    }

    
[Authorize]
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id)
 {
    var teacherId = User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(teacherId))
        return Unauthorized();

    var deleted = await _skillService.DeleteAsync(
        id, teacherId);

    if (!deleted)
    {
        return NotFound(new
        {
            message = "Skill not found or you do not own this listing."
        });
    }

    return Ok(new
    {
        message = "Skill deleted successfully."
    });
 }
}