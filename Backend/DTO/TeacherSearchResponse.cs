namespace Backend.DTOs;

public class TeacherSearchResponse
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
}
