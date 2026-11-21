using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly IExchangeService _service;
    public ExchangeController(IExchangeService service) => _service = service;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found.");

    [HttpGet("credits/wallet")]
    public async Task<IActionResult> Wallet(CancellationToken ct) => Ok(await _service.GetWalletAsync(UserId, ct));

    [HttpPost("credits/bookings")]
    public async Task<IActionResult> CreateCreditBooking([FromBody] CreateCreditBookingRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.CreateCreditBookingAsync(UserId, request, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message=ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }

    [HttpPut("credits/bookings/{id:int}/complete")]
    public async Task<IActionResult> CompleteCreditBooking(int id, CancellationToken ct)
    {
        try { return Ok(await _service.CompleteCreditBookingAsync(UserId, id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message=ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }

    [HttpPut("credits/bookings/{id:int}/cancel")]
    public async Task<IActionResult> CancelCreditBooking(int id, CancellationToken ct)
    {
        try { return Ok(await _service.CancelCreditBookingAsync(UserId, id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message=ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }

    [HttpGet("skill-swaps/me")]
    public async Task<IActionResult> MySwaps(CancellationToken ct) => Ok(await _service.GetMySkillSwapsAsync(UserId, ct));

    [HttpPost("skill-swaps")]
    public async Task<IActionResult> CreateSwap([FromBody] CreateSkillSwapRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.CreateSkillSwapAsync(UserId, request, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message=ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }

    [HttpPut("skill-swaps/{id:int}/accept")]
    public async Task<IActionResult> AcceptSwap(int id, CancellationToken ct) => await Respond(id, true, ct);
    [HttpPut("skill-swaps/{id:int}/reject")]
    public async Task<IActionResult> RejectSwap(int id, CancellationToken ct) => await Respond(id, false, ct);
    [HttpPut("skill-swaps/{id:int}/complete")]
    public async Task<IActionResult> CompleteSwap(int id, CancellationToken ct)
    {
        try { return Ok(await _service.CompleteSkillSwapAsync(UserId, id, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message=ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }
    private async Task<IActionResult> Respond(int id, bool accept, CancellationToken ct)
    {
        try { return Ok(await _service.RespondToSkillSwapAsync(UserId, id, accept, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message=ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message=ex.Message }); }
    }
}
