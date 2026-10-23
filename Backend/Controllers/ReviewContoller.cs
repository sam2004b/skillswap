
using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateReviewRequest request)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        try
        {
            var review = await _reviewService.CreateAsync(
                request, userId);

            return StatusCode(201, review);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("teacher/{teacherId}")]
    public async Task<IActionResult> GetTeacherReviews(
        string teacherId)
    {
        return Ok(
            await _reviewService.GetTeacherReviewsAsync(teacherId));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyReviews()
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        return Ok(await _reviewService.GetMyReviewsAsync(userId));
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, UpdateReviewRequest request)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        var review = await _reviewService.UpdateAsync(
            id, request, userId);

        return review == null ? NotFound() : Ok(review);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        var deleted = await _reviewService.DeleteAsync(id, userId);

        return deleted ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpGet("teacher/{teacherId}/rating")]
    public async Task<IActionResult> GetAverageRating(
        string teacherId)
    {
        var average = await _reviewService
            .GetAverageRatingAsync(teacherId);

        return Ok(new
        {
            teacherId,
            averageRating = average,
            totalReviews = await _reviewService
                .GetTeacherReviewsAsync(teacherId)
                .ContinueWith(t => t.Result.Count)
        });
    }
}
