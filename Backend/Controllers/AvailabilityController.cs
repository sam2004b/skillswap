using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/availability")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(
        IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    public async Task<ActionResult<AvailabilityResponse>> Create(
        CreateAvailabilityRequest request)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _availabilityService.CreateAsync(
                request, userId);

            return CreatedAtAction(
                nameof(GetMyAvailability),
                result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<AvailabilityResponse>>>
        GetMyAvailability()
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _availabilityService
            .GetMyAvailabilityAsync(userId);

        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<AvailabilityResponse>>>
        GetUserAvailability(string userId)
    {
        var result = await _availabilityService
            .GetUserAvailabilityAsync(userId);

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AvailabilityResponse>> Update(
        int id,
        UpdateAvailabilityRequest request)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _availabilityService.UpdateAsync(
                id, request, userId);

            if (result == null)
                return NotFound(new
                {
                    message = "Availability not found."
                });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var deleted = await _availabilityService.DeleteAsync(
            id, userId);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Availability not found."
            });
        }

        return Ok(new
        {
            message = "Availability deleted successfully."
        });
    }
}
