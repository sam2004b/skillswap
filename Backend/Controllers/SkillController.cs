using System.Security.Claims;
using Backend.DTOs.Skills;
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

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateSkillRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _skillService.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var skills = await _skillService.GetAllAsync();
        return Ok(skills);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var skill = await _skillService.GetByIdAsync(id);

        if (skill == null)
            return NotFound(new { message = "Skill not found." });

        return Ok(skill);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateSkillRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _skillService.UpdateAsync(
            id, request, userId);

        if (result == null)
            return NotFound(new
            {
                message = "Skill not found or you do not own this skill."
            });

        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var deleted = await _skillService.DeleteAsync(id, userId);

        if (!deleted)
            return NotFound(new
            {
                message = "Skill not found or you do not own this skill."
            });

        return Ok(new { message = "Skill deleted successfully." });
    }
}