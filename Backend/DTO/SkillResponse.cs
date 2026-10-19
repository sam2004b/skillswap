namespace Backend.DTOs;
public class SkillResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string TeacherId { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public decimal? HourlyRate { get; set; }

    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
