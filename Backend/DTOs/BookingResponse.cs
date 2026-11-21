namespace Backend.DTOs.Bookings;

public class BookingResponse
{
    public int Id { get; set; }

    public int SkillId { get; set; }

    public string SkillTitle { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string TeacherId { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateTimeOffset StartAtUtc { get; set; }

    public DateTimeOffset EndAtUtc { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal TotalPrice { get; set; }

    public string Currency { get; set; } = "USD";

    public string PaymentMethod { get; set; } = "Money";

    public decimal CreditCost { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}