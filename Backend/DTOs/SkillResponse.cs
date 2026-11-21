namespace Backend.DTOs.Skills;

public class SkillResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public decimal HourlyRate { get; set; }

    public string Currency { get; set; } = "USD";

    public decimal CreditRate { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}