
using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Models;

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    [Required]
    public string PayerId { get; set; } = string.Empty;

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    // Amount in the smallest currency unit (5000 = $50.00).
    public long AmountMinor { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "usd";

    // Pending, Paid, Failed, or Refunded.
    [Required, StringLength(20)]
    public string Status { get; set; } = "Pending";

    [StringLength(255)]
    public string? StripeCheckoutSessionId { get; set; }

    [StringLength(255)]
    public string? StripePaymentIntentId { get; set; }

    public long PlatformFeeMinor { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAtUtc { get; set; }
}
