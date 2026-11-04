namespace Backend.DTOs;

public class PaymentResponse
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string TeacherId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PlatformFeeAmount { get; set; }

    public decimal TeacherAmount { get; set; }

    public decimal RefundAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public DateTime? RefundedAtUtc { get; set; }
}