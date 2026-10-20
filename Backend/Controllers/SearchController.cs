
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("skills")]
    public async Task<ActionResult<List<SkillResponse>>> SearchSkills(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? country)
    {
        var results = await _searchService.SearchSkillsAsync(
            search, category, country);

        return Ok(results);
    }

    [HttpGet("teachers")]
    public async Task<ActionResult<List<TeacherSearchResponse>>> SearchTeachers(
        [FromQuery] string? skill,
        [FromQuery] string? category,
        [FromQuery] string? country)
    {
        var results = await _searchService.SearchTeachersAsync(
            skill, category, country);

        return Ok(results);
    }

    [HttpGet("teachers/{id}")]
    public async Task<ActionResult<TeacherProfileResponse>> GetTeacherProfile(
        string id)
    {
        var profile = await _searchService.GetTeacherProfileAsync(id);

        if (profile == null)
            return NotFound(new { message = "Teacher not found." });

        return Ok(profile);
    }
}
