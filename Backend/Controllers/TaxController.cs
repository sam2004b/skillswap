using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/tax")]
[Authorize]
public class TaxController : ControllerBase
{
    private readonly ITaxService _taxService;

    public TaxController(ITaxService taxService)
    {
        _taxService = taxService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found.");

    [HttpPost("profile")]
    public async Task<IActionResult> SaveProfile(
        [FromBody] TaxProfileRequest request,
        CancellationToken cancellationToken)
    {
        await _taxService.SaveTaxProfileAsync(
            CurrentUserId,
            request,
            cancellationToken);

        return Ok(new
        {
            message = "Tax profile saved successfully."
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(
        CancellationToken cancellationToken)
    {
        var profile = await _taxService.GetTaxProfileAsync(
            CurrentUserId,
            cancellationToken);

        if (profile == null)
            return NotFound(new
            {
                message = "Tax profile not found."
            });

        return Ok(profile);
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateTax(
        [FromBody] TaxCalculationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _taxService.CalculateTaxAsync(
                request.BookingId,
                CurrentUserId,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
