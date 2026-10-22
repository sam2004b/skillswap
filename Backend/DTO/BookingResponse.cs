
namespace Backend.DTOs;

public class BookingResponse
{
    public int Id { get; set; }
    public string LearnerId { get; set; } = string.Empty;
    public string LearnerName { get; set; } = string.Empty;
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BookingType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
