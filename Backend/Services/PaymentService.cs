using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Backend.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<PaymentResponse> CreateCheckoutAsync(
        CreatePaymentRequest request,
        string userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking is null)
            throw new ArgumentException("Booking not found.");

        if (booking.LearnerId != userId)
            throw new UnauthorizedAccessException(
                "You can only pay for your own bookings.");

        if (!string.Equals(
                booking.BookingType,
                "Paid",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "This booking is not a paid booking.");
        }

        if (!string.Equals(
                booking.Status,
                "Confirmed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only confirmed bookings can be paid for.");
        }

        var existingPayments = await _context.Payments
            .Where(p => p.BookingId == booking.Id &&
                        p.PayerId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        // Check existing payment records with Stripe before allowing
        // another checkout attempt.
        foreach (var existingPayment in existingPayments)
        {
            if (existingPayment.Status == "Paid")
            {
                throw new ArgumentException(
                    "This booking has already been paid for.");
            }

            if (existingPayment.Status != "Pending" ||
                string.IsNullOrWhiteSpace(
                    existingPayment.StripeCheckoutSessionId))
            {
                continue;
            }

            var existingSession = await new SessionService().GetAsync(
                existingPayment.StripeCheckoutSessionId);

            if (existingSession.PaymentStatus == "paid")
            {
                if (existingSession.AmountTotal !=
                        existingPayment.AmountMinor ||
                    !string.Equals(
                        existingSession.Currency,
                        existingPayment.Currency,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Stripe payment amount or currency mismatch.");
                }

                existingPayment.Status = "Paid";
                existingPayment.StripePaymentIntentId =
                    existingSession.PaymentIntentId;
                existingPayment.PaidAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                throw new ArgumentException(
                    "This booking has already been paid for.");
            }

            if (existingSession.Status == "open" &&
                !string.IsNullOrWhiteSpace(existingSession.Url))
            {
                return ToResponse(
                    existingPayment,
                    existingSession.Url);
            }

            // Only mark an expired, unpaid session as failed.
            if (existingSession.Status == "expired")
            {
                existingPayment.Status = "Failed";
                await _context.SaveChangesAsync();
            }
        }

        var amountMinor = _configuration.GetValue<long>(
            "Payments:SessionPriceMinor");

        if (amountMinor <= 0)
        {
            throw new InvalidOperationException(
                "Payments:SessionPriceMinor must be configured.");
        }

        var currency = (
            _configuration["Payments:Currency"] ?? "usd"
        ).ToLowerInvariant();

        var feePercent = _configuration.GetValue<decimal>(
            "Payments:PlatformFeePercent");

        if (feePercent < 0 || feePercent > 100)
        {
            throw new InvalidOperationException(
                "Platform fee percentage must be between 0 and 100.");
        }

        var platformFeeMinor = (long)Math.Round(
            amountMinor * feePercent / 100m,
            MidpointRounding.AwayFromZero);

        var payment = new Payment
        {
            BookingId = booking.Id,
            PayerId = booking.LearnerId,
            TeacherId = booking.TeacherId,
            AmountMinor = amountMinor,
            Currency = currency,
            PlatformFeeMinor = platformFeeMinor,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var frontendUrl = _configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException(
                "Frontend:BaseUrl must be configured.");
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = payment.Id.ToString(),

            SuccessUrl =
                $"{frontendUrl.TrimEnd('/')}/payment/success" +
                "?session_id={CHECKOUT_SESSION_ID}",

            CancelUrl =
                $"{frontendUrl.TrimEnd('/')}/payment/cancel",

            Metadata = new Dictionary<string, string>
            {
                ["paymentId"] = payment.Id.ToString(),
                ["bookingId"] = booking.Id.ToString()
            },

            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = amountMinor,
                        ProductData =
                            new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "SkillSwap paid skill session",
                                Description = $"Booking #{booking.Id}"
                            }
                    }
                }
            }
        };

        try
        {
            var session = await new SessionService().CreateAsync(
                options,
                new RequestOptions
                {
                    IdempotencyKey = $"skillswap-payment-{payment.Id}"
                });

            payment.StripeCheckoutSessionId = session.Id;
            await _context.SaveChangesAsync();

            return ToResponse(payment, session.Url);
        }
        catch
        {
            payment.Status = "Failed";
            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task<List<PaymentResponse>> GetMyPaymentsAsync(
        string userId)
    {
        var payments = await _context.Payments
            .Where(p => p.PayerId == userId ||
                        p.TeacherId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        var responses = new List<PaymentResponse>();

        foreach (var payment in payments)
        {
            if (payment.Status == "Pending")
            {
                await RefreshPaymentStatusAsync(payment);
            }

            responses.Add(ToResponse(payment));
        }

        return responses;
    }

    public async Task<PaymentResponse?> GetPaymentAsync(
        int id,
        string userId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                (p.PayerId == userId || p.TeacherId == userId));

        if (payment is null)
            return null;

        if (payment.Status == "Pending")
        {
            await RefreshPaymentStatusAsync(payment);
        }

        return ToResponse(payment);
    }

    private async Task RefreshPaymentStatusAsync(Payment payment)
    {
        if (string.IsNullOrWhiteSpace(
                payment.StripeCheckoutSessionId))
        {
            return;
        }

        var session = await new SessionService().GetAsync(
            payment.StripeCheckoutSessionId);

        // Never mark a payment Paid based only on the redirect URL.
        if (session.PaymentStatus != "paid")
            return;

        if (session.AmountTotal != payment.AmountMinor ||
            !string.Equals(
                session.Currency,
                payment.Currency,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Stripe amount or currency mismatch for payment {payment.Id}.");
        }

        payment.Status = "Paid";
        payment.StripePaymentIntentId = session.PaymentIntentId;
        payment.PaidAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static PaymentResponse ToResponse(
        Payment payment,
        string? checkoutUrl = null)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            AmountMinor = payment.AmountMinor,
            Currency = payment.Currency,
            Status = payment.Status,
            PlatformFeeMinor = payment.PlatformFeeMinor,
            CheckoutUrl = checkoutUrl,
            CreatedAtUtc = payment.CreatedAtUtc,
            PaidAtUtc = payment.PaidAtUtc
        };
    }
}