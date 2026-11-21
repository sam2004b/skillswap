using Backend.Data;
using Backend.DTOs.Skills;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SkillService : ISkillService
{
    private readonly ApplicationDbContext _context;

    public SkillService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SkillResponse> CreateAsync(
        CreateSkillRequest request,
        string userId)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.Id == userId);

        if (!userExists)
            throw new UnauthorizedAccessException("User not found.");

        var skill = new Skill
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Level = request.Level.Trim(),
            HourlyRate = request.HourlyRate,
            CreditRate = request.CreditRate,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(skill.Id)
            ?? throw new InvalidOperationException(
                "Could not retrieve the created skill.");
    }

    public async Task<List<SkillResponse>> GetAllAsync()
    {
        return await _context.Skills
            .AsNoTracking()
            .Include(s => s.User)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Category = s.Category,
                Level = s.Level,
                HourlyRate = s.HourlyRate,
                CreditRate = s.CreditRate,
                Currency = s.Currency,
                UserId = s.UserId,
                TeacherName = s.User != null ? s.User.FullName : "",
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<SkillResponse?> GetByIdAsync(int id)
    {
        return await _context.Skills
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Category = s.Category,
                Level = s.Level,
                HourlyRate = s.HourlyRate,
                CreditRate = s.CreditRate,
                Currency = s.Currency,
                UserId = s.UserId,
                TeacherName = s.User != null ? s.User.FullName : "",
                CreatedAtUtc = s.CreatedAtUtc
            })
            .FirstOrDefaultAsync();
    }

    public async Task<SkillResponse?> UpdateAsync(
        int id,
        UpdateSkillRequest request,
        string userId)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id);

        if (skill == null || skill.UserId != userId)
            return null;

        skill.Title = request.Title.Trim();
        skill.Description = request.Description.Trim();
        skill.Category = request.Category.Trim();
        skill.Level = request.Level.Trim();
        skill.HourlyRate = request.HourlyRate;
        skill.CreditRate = request.CreditRate;
        skill.Currency = request.Currency.Trim().ToUpperInvariant();
        skill.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.Id == id);

        if (skill == null || skill.UserId != userId)
            return false;

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();

        return true;
    }
}