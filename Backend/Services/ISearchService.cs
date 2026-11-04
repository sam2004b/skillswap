using Backend.DTOs.Search;
using Backend.DTOs.Skills;

namespace Backend.Services;

public interface ISearchService
{
    Task<List<SkillResponse>> SearchSkillsAsync(
        string? keyword,
        string? category,
        string? level,
        decimal? minHourlyRate,
        decimal? maxHourlyRate);

    Task<List<TeacherSearchResponse>> SearchTeachersAsync(
        string? keyword,
        string? category,
        string? level);

    Task<TeacherDetailsResponse?> GetTeacherByIdAsync(string userId);
}