
namespace Backend.DTOs;

public class ReviewResponse
{
    public int Id { get; set; }
    public int BookingId { get; set; }

    public string ReviewerId { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;

    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
