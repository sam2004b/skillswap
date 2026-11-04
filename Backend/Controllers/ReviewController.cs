using Backend.DTOs.Reviews;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(
                "User ID not found in token.");
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request)
    {
        try
        {
            var result = await _reviewService.CreateReviewAsync(
                GetUserId(), request);

            return CreatedAtAction(
                nameof(GetReviewById),
                new { id = result.Id },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("teacher/{teacherId}")]
    public async Task<IActionResult> GetTeacherReviews(string teacherId)
    {
        try
        {
            var result = await _reviewService.GetTeacherReviewsAsync(
                teacherId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyReviews()
    {
        var result = await _reviewService.GetMyReviewsAsync(
            GetUserId());

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReviewById(int id)
    {
        var result = await _reviewService.GetReviewByIdAsync(id);

        if (result == null)
        {
            return NotFound(new { message = "Review not found." });
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        try
        {
            await _reviewService.DeleteReviewAsync(id, GetUserId());

            return Ok(new
            {
                message = "Review deleted successfully."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}