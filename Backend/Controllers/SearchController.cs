using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/search")]
[AllowAnonymous]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("skills")]
    public async Task<IActionResult> SearchSkills(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] string? level,
        [FromQuery] decimal? minHourlyRate,
        [FromQuery] decimal? maxHourlyRate)
    {
        if (minHourlyRate < 0 || maxHourlyRate < 0)
        {
            return BadRequest(new
            {
                message = "Hourly rate filters cannot be negative."
            });
        }

        if (minHourlyRate.HasValue &&
            maxHourlyRate.HasValue &&
            minHourlyRate.Value > maxHourlyRate.Value)
        {
            return BadRequest(new
            {
                message = "Minimum hourly rate cannot exceed maximum hourly rate."
            });
        }

        var skills = await _searchService.SearchSkillsAsync(
            keyword,
            category,
            level,
            minHourlyRate,
            maxHourlyRate);

        return Ok(skills);
    }

    [HttpGet("teachers")]
    public async Task<IActionResult> SearchTeachers(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] string? level)
    {
        var teachers = await _searchService.SearchTeachersAsync(
            keyword,
            category,
            level);

        return Ok(teachers);
    }

    [HttpGet("teachers/{id}")]
    public async Task<IActionResult> GetTeacherById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new
            {
                message = "Teacher ID is required."
            });
        }

        var teacher = await _searchService.GetTeacherByIdAsync(id);

        if (teacher == null)
        {
            return NotFound(new
            {
                message = "Teacher not found."
            });
        }

        return Ok(teacher);
    }
}