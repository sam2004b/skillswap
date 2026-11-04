namespace Backend.DTOs.Search;

public class TeacherSearchResponse
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Country { get; set; }

    public int SkillCount { get; set; }

    public List<string> Skills { get; set; } = new();
}