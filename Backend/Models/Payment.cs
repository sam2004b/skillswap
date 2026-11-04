using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    public Booking? Booking { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public AuthenticationUser? Student { get; set; }

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public AuthenticationUser? Teacher { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal PlatformFeeAmount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TeacherAmount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal RefundAmount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    [MaxLength(255)]
    public string? StripeCheckoutSessionId { get; set; }

    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }

    [MaxLength(255)]
    public string? StripeRefundId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAtUtc { get; set; }

    public DateTime? RefundedAtUtc { get; set; }
}