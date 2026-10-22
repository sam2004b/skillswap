using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateBookingRequest request)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        try
        {
            var booking = await _bookingService.CreateAsync(
                request, userId);

            return StatusCode(201, booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyBookings()
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        return Ok(
            await _bookingService.GetMyBookingsAsync(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        var booking = await _bookingService.GetByIdAsync(
            id, userId);

        return booking == null ? NotFound() : Ok(booking);
    }

    [HttpPut("{id:int}/reschedule")]
    public async Task<IActionResult> Reschedule(
        int id, RescheduleBookingRequest request)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        try
        {
            var booking = await _bookingService.RescheduleAsync(
                id, request, userId);

            return booking == null ? NotFound() : Ok(booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        try
        {
            var booking = await _bookingService.CancelAsync(
                id, userId);

            return booking == null ? NotFound() : Ok(booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        try
        {
            var booking = await _bookingService.CompleteAsync(
                id, userId);

            return booking == null ? NotFound() : Ok(booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
