using System.Globalization;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Backend.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly StripeClient _stripe;
    private readonly IConfiguration _configuration;

    public PaymentService(
        ApplicationDbContext db,
        StripeClient stripe,
        IConfiguration configuration)
    {
        _db = db;
        _stripe = stripe;
        _configuration = configuration;
    }

    private decimal FeePercentage =>
        _configuration.GetValue<decimal>(
            "Stripe:PlatformFeePercentage", 10m);

    private static long ToMinorUnits(decimal amount, string currency)
    {
        int exponent = currency.ToLowerInvariant() switch
        {
            "bif" or "clp" or "djf" or "gnf" or "jpy" or
            "kmf" or "krw" or "mga" or "pyg" or "rwf" or
            "ugx" or "vnd" or "vuv" or "xaf" or "xof" or
            "xpf" => 0,

            "bhd" or "iqd" or "jod" or "kwd" or "lyd" or
            "omr" or "tnd" => 3,

            _ => 2
        };

        decimal multiplier = exponent switch
        {
            0 => 1m,
            3 => 1000m,
            _ => 100m
        };

        decimal scaled = amount * multiplier;

        if (scaled != decimal.Truncate(scaled))
            throw new InvalidOperationException(
                "Amount has too many decimal places for this currency.");

        return checked((long)scaled);
    }

    private static PaymentResponse MapPayment(Payment p)
    {
        return new PaymentResponse
        {
            Id = p.Id,
            BookingId = p.BookingId,
            StudentId = p.StudentId,
            TeacherId = p.TeacherId,
            Amount = p.Amount,
            TaxAmount = p.TaxAmount,
            TotalAmount = p.TotalAmount,
            PlatformFeeAmount = p.PlatformFeeAmount,
            TeacherAmount = p.TeacherAmount,
            RefundAmount = p.RefundAmount,
            Currency = p.Currency,
            Status = p.Status,
            CreatedAtUtc = p.CreatedAtUtc,
            PaidAtUtc = p.PaidAtUtc,
            RefundedAtUtc = p.RefundedAtUtc
        };
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string studentId,
        int bookingId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Student)
            .Include(b => b.Teacher)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new KeyNotFoundException("Booking not found.");

        if (booking.StudentId != studentId)
            throw new UnauthorizedAccessException(
                "You cannot pay for another student's booking.");

        if (!string.Equals(
                booking.Status,
                "Confirmed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only confirmed bookings can be paid.");
        }

        if (booking.TotalPrice <= 0)
            throw new InvalidOperationException(
                "Booking amount must be greater than zero.");

        if (booking.Teacher == null ||
            string.IsNullOrWhiteSpace(booking.Teacher.StripeAccountId))
        {
            throw new InvalidOperationException(
                "Teacher has not connected a Stripe account.");
        }

        if (!booking.Teacher.StripeChargesEnabled ||
            !booking.Teacher.StripePayoutsEnabled)
        {
            throw new InvalidOperationException(
                "Teacher Stripe account is not ready.");
        }

        string currency = booking.Currency.ToLowerInvariant();

        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);

        if (payment != null &&
            payment.Status is "Succeeded" or "Refunded" or "PartiallyRefunded")
        {
            throw new InvalidOperationException(
                "This booking already has a completed payment.");
        }

        if (payment != null &&
            payment.Status == "Pending" &&
            !string.IsNullOrWhiteSpace(payment.StripeCheckoutSessionId))
        {
            var existingSession = await new SessionService(_stripe)
                .GetAsync(payment.StripeCheckoutSessionId);

            if (existingSession.Status == "open" &&
                !string.IsNullOrWhiteSpace(existingSession.Url))
            {
                return existingSession.Url;
            }

            if (existingSession.Status == "complete")
            {
                throw new InvalidOperationException(
                    "Payment is being processed. Please wait for confirmation.");
            }
        }

        decimal amount = booking.TotalPrice;
        decimal tax = 0m;
        decimal total = amount + tax;

        if (FeePercentage < 0 || FeePercentage >= 100)
            throw new InvalidOperationException(
                "Platform fee percentage must be between 0 and 100.");

        decimal platformFee = Math.Round(
            total * FeePercentage / 100m,
            2,
            MidpointRounding.AwayFromZero);

        decimal teacherAmount = total - platformFee;

        long totalMinor = ToMinorUnits(total, currency);
        long feeMinor = ToMinorUnits(platformFee, currency);

        if (totalMinor <= 0 || feeMinor >= totalMinor)
            throw new InvalidOperationException(
                "Invalid payment or platform fee.");

        if (payment == null)
        {
            payment = new Payment
            {
                BookingId = booking.Id,
                StudentId = booking.StudentId,
                TeacherId = booking.TeacherId,
                Amount = amount,
                TaxAmount = tax,
                TotalAmount = total,
                PlatformFeeAmount = platformFee,
                TeacherAmount = teacherAmount,
                Currency = booking.Currency.ToUpperInvariant(),
                Status = "Pending"
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
        }
        else
        {
            payment.Amount = amount;
            payment.TaxAmount = tax;
            payment.TotalAmount = total;
            payment.PlatformFeeAmount = platformFee;
            payment.TeacherAmount = teacherAmount;
            payment.Currency = booking.Currency.ToUpperInvariant();
            payment.Status = "Pending";
            payment.StripeCheckoutSessionId = null;
            payment.StripePaymentIntentId = null;

            await _db.SaveChangesAsync();
        }

        var successUrl = _configuration["Stripe:SuccessUrl"]
            ?? throw new InvalidOperationException(
                "Stripe success URL is missing.");

        var cancelUrl = _configuration["Stripe:CancelUrl"]
            ?? throw new InvalidOperationException(
                "Stripe cancel URL is missing.");

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = payment.Id.ToString(
                CultureInfo.InvariantCulture),
            CustomerEmail = booking.Student?.Email,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = totalMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "SkillSwap Booking",
                            Description = $"Booking #{booking.Id}"
                        }
                    }
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = feeMinor,
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = booking.Teacher.StripeAccountId
                },
                Metadata = new Dictionary<string, string>
                {
                    ["PaymentId"] = payment.Id.ToString(),
                    ["BookingId"] = booking.Id.ToString()
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["PaymentId"] = payment.Id.ToString(),
                ["BookingId"] = booking.Id.ToString()
            }
        };

        var session = await new SessionService(_stripe)
            .CreateAsync(options);

        payment.StripeCheckoutSessionId = session.Id;
        await _db.SaveChangesAsync();

        return session.Url;
    }

    public async Task<List<PaymentResponse>> GetMyPaymentsAsync(
        string userId)
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.StudentId == userId ||
                        p.TeacherId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        return payments.Select(MapPayment).ToList();
    }

    public async Task<PaymentResponse?> GetPaymentByIdAsync(
        string userId,
        int paymentId)
    {
        var payment = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == paymentId &&
                (p.StudentId == userId || p.TeacherId == userId));

        return payment == null ? null : MapPayment(payment);
    }

    public async Task<PaymentResponse> RefundPaymentAsync(
        string userId,
        int paymentId)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new KeyNotFoundException("Payment not found.");

        if (payment.StudentId != userId)
            throw new UnauthorizedAccessException(
                "Only the student who paid can request this refund.");

        if (payment.Status != "Succeeded")
            throw new InvalidOperationException(
                "Only successful payments can be refunded.");

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            throw new InvalidOperationException(
                "Stripe payment intent was not found.");

        var refund = await new RefundService(_stripe).CreateAsync(
            new RefundCreateOptions
            {
                PaymentIntent = payment.StripePaymentIntentId,
                ReverseTransfer = true,
                RefundApplicationFee = true
            });

        payment.StripeRefundId = refund.Id;
        payment.Status = refund.Status == "succeeded"
            ? "Refunded"
            : "RefundPending";

        if (refund.Status == "succeeded")
        {
            payment.RefundAmount = payment.TotalAmount;
            payment.RefundedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return MapPayment(payment);
    }

    public async Task<StripeConnectResponse>
        CreateOrRefreshConnectOnboardingAsync(string teacherId)
    {
        var teacher = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == teacherId);

        if (teacher == null)
            throw new KeyNotFoundException("Teacher not found.");

        if (string.IsNullOrWhiteSpace(teacher.Country) ||
            teacher.Country.Length != 2)
        {
            throw new InvalidOperationException(
                "Teacher Country must contain a valid two-letter country code, such as IE, US or GB.");
        }

        if (string.IsNullOrWhiteSpace(teacher.StripeAccountId))
        {
            var account = await new AccountService(_stripe).CreateAsync(
                new AccountCreateOptions
                {
                    Type = "express",
                    Country = teacher.Country.ToUpperInvariant(),
                    Email = teacher.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions
                        {
                            Requested = true
                        },
                        Transfers = new AccountCapabilitiesTransfersOptions
                        {
                            Requested = true
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["SkillSwapUserId"] = teacher.Id
                    }
                });

            teacher.StripeAccountId = account.Id;
            await _db.SaveChangesAsync();
        }

        var refreshUrl = _configuration["Stripe:RefreshUrl"]
            ?? throw new InvalidOperationException(
                "Stripe refresh URL is missing.");

        var returnUrl = _configuration["Stripe:ReturnUrl"]
            ?? throw new InvalidOperationException(
                "Stripe return URL is missing.");

        var link = await new AccountLinkService(_stripe).CreateAsync(
            new AccountLinkCreateOptions
            {
                Account = teacher.StripeAccountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding"
            });

        return new StripeConnectResponse
        {
            Url = link.Url,
            StripeAccountId = teacher.StripeAccountId,
            OnboardingComplete = teacher.StripeOnboardingComplete,
            ChargesEnabled = teacher.StripeChargesEnabled,
            PayoutsEnabled = teacher.StripePayoutsEnabled
        };
    }

    public async Task<StripeConnectResponse> GetConnectStatusAsync(
        string teacherId)
    {
        var teacher = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == teacherId);

        if (teacher == null)
            throw new KeyNotFoundException("Teacher not found.");

        if (string.IsNullOrWhiteSpace(teacher.StripeAccountId))
            return new StripeConnectResponse();

        var account = await new AccountService(_stripe)
            .GetAsync(teacher.StripeAccountId);

        teacher.StripeChargesEnabled = account.ChargesEnabled;
        teacher.StripePayoutsEnabled = account.PayoutsEnabled;
        teacher.StripeOnboardingComplete =
            account.DetailsSubmitted &&
            account.ChargesEnabled &&
            account.PayoutsEnabled;

        await _db.SaveChangesAsync();

        return new StripeConnectResponse
        {
            StripeAccountId = account.Id,
            OnboardingComplete = teacher.StripeOnboardingComplete,
            ChargesEnabled = account.ChargesEnabled,
            PayoutsEnabled = account.PayoutsEnabled
        };
    }

    public async Task ProcessWebhookAsync(
        string json,
        string signature)
    {
        var secret = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Stripe webhook secret is not configured.");

        var stripeEvent = EventUtility.ConstructEvent(
            json,
            signature,
            secret);

        if (await _db.StripeWebhookEvents.AnyAsync(
            e => e.StripeEventId == stripeEvent.Id))
        {
            return;
        }

        var webhookRecord = new StripeWebhookEvent
        {
            StripeEventId = stripeEvent.Id,
            EventType = stripeEvent.Type
        };

        _db.StripeWebhookEvents.Add(webhookRecord);
        await _db.SaveChangesAsync();

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session?.PaymentStatus == "paid")
                    {
                        var payment = await FindPaymentForSessionAsync(session);

                        if (payment != null)
                        {
                            payment.Status = "Succeeded";
                            payment.PaidAtUtc ??= DateTime.UtcNow;
                            payment.StripePaymentIntentId =
                                session.PaymentIntentId;
                        }
                    }

                    break;
                }

                case "checkout.session.async_payment_failed":
                case "checkout.session.expired":
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session != null)
                    {
                        var payment = await FindPaymentForSessionAsync(session);

                        if (payment != null &&
                            payment.Status == "Pending")
                        {
                            payment.Status = "Failed";
                        }
                    }

                    break;
                }

                case "payment_intent.payment_failed":
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;

                    if (intent != null)
                    {
                        Payment? payment = null;

                        if (intent.Metadata != null &&
                            intent.Metadata.TryGetValue(
                                "PaymentId", out var idText) &&
                            int.TryParse(idText, out var id))
                        {
                            payment = await _db.Payments
                                .FirstOrDefaultAsync(p => p.Id == id);
                        }

                        payment ??= await _db.Payments
                            .FirstOrDefaultAsync(p =>
                                p.StripePaymentIntentId == intent.Id);

                        if (payment != null &&
                            payment.Status == "Pending")
                        {
                            payment.Status = "Failed";
                        }
                    }

                    break;
                }

                case "charge.refunded":
                {
                    var charge = stripeEvent.Data.Object as Charge;

                    if (charge?.PaymentIntentId != null)
                    {
                        var payment = await _db.Payments
                            .FirstOrDefaultAsync(p =>
                                p.StripePaymentIntentId ==
                                charge.PaymentIntentId);

                        if (payment != null)
                        {
                            payment.RefundAmount =
                                charge.AmountRefunded /
                                (decimal)Math.Pow(
                                    10,
                                    CurrencyExponent(payment.Currency));

                            payment.Status = charge.Refunded
                                ? "Refunded"
                                : "PartiallyRefunded";

                            if (charge.Refunded)
                                payment.RefundedAtUtc = DateTime.UtcNow;
                        }
                    }

                    break;
                }
            }

            webhookRecord.ProcessedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        catch
        {
            _db.StripeWebhookEvents.Remove(webhookRecord);
            await _db.SaveChangesAsync();
            throw;
        }
    }

    private async Task<Payment?> FindPaymentForSessionAsync(
        Session session)
    {
        if (session.Metadata != null &&
            session.Metadata.TryGetValue("PaymentId", out var idText) &&
            int.TryParse(idText, out var id))
        {
            return await _db.Payments
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        return await _db.Payments.FirstOrDefaultAsync(
            p => p.StripeCheckoutSessionId == session.Id);
    }

    private static int CurrencyExponent(string currency)
    {
        return currency.ToLowerInvariant() switch
        {
            "bif" or "clp" or "djf" or "gnf" or "jpy" or
            "kmf" or "krw" or "mga" or "pyg" or "rwf" or
            "ugx" or "vnd" or "vuv" or "xaf" or "xof" or
            "xpf" => 0,

            "bhd" or "iqd" or "jod" or "kwd" or "lyd" or
            "omr" or "tnd" => 3,

            _ => 2
        };
    }
}