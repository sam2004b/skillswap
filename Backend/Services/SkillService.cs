using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend.Services;

public class SkillService : ISkillService
{
    private readonly ApplicationDbContext _db;

    public SkillService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SkillResponse> CreateAsync(
        CreateSkillRequest request,
        string teacherId)
    {
        var skill = new Skill
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            TeacherId = teacherId,
            HourlyRate = request.HourlyRate,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Skills.Add(skill);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(skill.Id)
            ?? throw new InvalidOperationException(
                "The skill was created but could not be retrieved.");
    }

    public async Task<List<SkillResponse>> GetAllAsync(
        string? search,
        string? category)
    {
        var query = _db.Skills
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(s =>
                s.Name.Contains(term) ||
                s.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var selectedCategory = category.Trim();

            query = query.Where(s =>
                s.Category.ToLower() == selectedCategory.ToLower());
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher != null
                    ? s.Teacher.FullName
                    : string.Empty,
                HourlyRate = s.HourlyRate,
                Currency = s.Currency,
                IsActive = s.IsActive,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<SkillResponse?> GetByIdAsync(int id)
    {
        return await _db.Skills
            .AsNoTracking()
            .Where(s => s.Id == id && s.IsActive)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher != null
                    ? s.Teacher.FullName
                    : string.Empty,
                HourlyRate = s.HourlyRate,
                Currency = s.Currency,
                IsActive = s.IsActive,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .FirstOrDefaultAsync();
    }

    public async Task<SkillResponse?> UpdateAsync(
        int id,
        UpdateSkillRequest request,
        string teacherId)
    {
        var skill = await _db.Skills.FirstOrDefaultAsync(s =>
            s.Id == id && s.TeacherId == teacherId);

        if (skill is null)
            return null;

        skill.Name = request.Name.Trim();
        skill.Description = request.Description.Trim();
        skill.Category = request.Category.Trim();
        skill.HourlyRate = request.HourlyRate;
        skill.Currency = request.Currency.Trim().ToUpperInvariant();
        skill.IsActive = request.IsActive;
        skill.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, string teacherId)
    {
        var skill = await _db.Skills.FirstOrDefaultAsync(s =>
            s.Id == id && s.TeacherId == teacherId);

        if (skill is null)
            return false;

        // Soft delete: preserve the record for future bookings/history.
        skill.IsActive = false;
        skill.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
