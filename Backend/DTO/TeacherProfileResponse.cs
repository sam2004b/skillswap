namespace Backend.DTOs;
public class TeacherProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public List<SkillResponse> Skills { get; set; } = new();
}
