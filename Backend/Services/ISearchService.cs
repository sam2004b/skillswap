using Backend.DTOs;
namespace Backend.Services;

public interface ISearchService
{
    Task<List<SkillResponse>> SearchSkillsAsync(
        string? search,
        string? category,
        string? country);

    Task<List<TeacherSearchResponse>> SearchTeachersAsync(
        string? skill,
        string? category,
        string? country);

    Task<TeacherProfileResponse?> GetTeacherProfileAsync(string teacherId);
}
