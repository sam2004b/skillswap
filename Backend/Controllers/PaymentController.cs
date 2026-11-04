using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Backend.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found.");

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            var url = await _paymentService.CreateCheckoutSessionAsync(
                CurrentUserId,
                request.BookingId);

            return Ok(new { checkoutUrl = url });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (StripeException)
        {
            return StatusCode(502, new
            {
                message = "Stripe could not create the checkout session."
            });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyPayments()
    {
        var payments = await _paymentService.GetMyPaymentsAsync(
            CurrentUserId);

        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(
            CurrentUserId,
            id);

        if (payment == null)
            return NotFound(new { message = "Payment not found." });

        return Ok(payment);
    }

    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> RefundPayment(int id)
    {
        try
        {
            var payment = await _paymentService.RefundPaymentAsync(
                CurrentUserId,
                id);

            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (StripeException)
        {
            return StatusCode(502, new
            {
                message = "Stripe could not process the refund."
            });
        }
    }

    [HttpPost("connect/onboard")]
    public async Task<IActionResult> ConnectOnboarding()
    {
        try
        {
            var result =
                await _paymentService.CreateOrRefreshConnectOnboardingAsync(
                    CurrentUserId);

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
        catch (StripeException)
        {
            return StatusCode(502, new
            {
                message = "Stripe could not create the onboarding link."
            });
        }
    }

    [HttpGet("connect/status")]
    public async Task<IActionResult> ConnectStatus()
    {
        try
        {
            return Ok(await _paymentService.GetConnectStatusAsync(
                CurrentUserId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (StripeException)
        {
            return StatusCode(502, new
            {
                message = "Stripe account status could not be retrieved."
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest("Missing Stripe-Signature header.");

        try
        {
            await _paymentService.ProcessWebhookAsync(json, signature);
            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest("Invalid Stripe webhook signature.");
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500, "Webhook configuration error.");
        }
    }
}