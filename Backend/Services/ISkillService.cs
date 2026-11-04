using Backend.DTOs.Skills;

namespace Backend.Services;

public interface ISkillService
{
    Task<SkillResponse> CreateAsync(
        CreateSkillRequest request,
        string userId);

    Task<List<SkillResponse>> GetAllAsync();

    Task<SkillResponse?> GetByIdAsync(int id);

    Task<SkillResponse?> UpdateAsync(
        int id,
        UpdateSkillRequest request,
        string userId);

    Task<bool> DeleteAsync(int id, string userId);
}