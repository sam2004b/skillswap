using Backend.DTOs;
using Backend.DTOs.Skills;

namespace Backend.DTOs.Search;

public class TeacherDetailsResponse
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Country { get; set; }

    public List<SkillResponse> Skills { get; set; } = new();

    public List<AvailabilityResponse> Availability { get; set; } = new();
}