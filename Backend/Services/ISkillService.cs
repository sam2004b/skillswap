using Backend.DTOs;
namespace Backend.Services;
public interface ISkillService
{
    Task<SkillResponse> CreateAsync(
        CreateSkillRequest request,
        string teacherId);

    Task<List<SkillResponse>> GetAllAsync(
        string? search,
        string? category);

    Task<SkillResponse?> GetByIdAsync(int id);

    Task<SkillResponse?> UpdateAsync(
        int id,
        UpdateSkillRequest request,
        string teacherId);

    Task<bool> DeleteAsync(int id, string teacherId);
}
