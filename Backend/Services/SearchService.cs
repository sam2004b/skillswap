using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Search;
using Backend.DTOs.Skills;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SkillResponse>> SearchSkillsAsync(
        string? keyword,
        string? category,
        string? level,
        decimal? minHourlyRate,
        decimal? maxHourlyRate)
    {
        var query = _context.Skills
            .AsNoTracking()
            .Where(s => s.User != null && s.User.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";

            query = query.Where(s =>
                EF.Functions.ILike(s.Title, pattern) ||
                EF.Functions.ILike(s.Description, pattern) ||
                EF.Functions.ILike(s.Category, pattern));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryPattern = $"%{category.Trim()}%";

            query = query.Where(s =>
                EF.Functions.ILike(s.Category, categoryPattern));
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(s => s.Level == level.Trim());
        }

        if (minHourlyRate.HasValue)
        {
            query = query.Where(s =>
                s.HourlyRate >= minHourlyRate.Value);
        }

        if (maxHourlyRate.HasValue)
        {
            query = query.Where(s =>
                s.HourlyRate <= maxHourlyRate.Value);
        }

        return await query
            .OrderBy(s => s.HourlyRate)
            .ThenBy(s => s.Title)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Category = s.Category,
                Level = s.Level,
                HourlyRate = s.HourlyRate,
                Currency = s.Currency,
                UserId = s.UserId,
                TeacherName = s.User != null
                    ? s.User.FullName
                    : string.Empty,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<List<TeacherSearchResponse>> SearchTeachersAsync(
        string? keyword,
        string? category,
        string? level)
    {
        var usersQuery = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";

            usersQuery = usersQuery.Where(u =>
                EF.Functions.ILike(u.FullName, pattern) ||
                _context.Skills.Any(s =>
                    s.UserId == u.Id &&
                    EF.Functions.ILike(s.Title, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryPattern = $"%{category.Trim()}%";

            usersQuery = usersQuery.Where(u =>
                _context.Skills.Any(s =>
                    s.UserId == u.Id &&
                    EF.Functions.ILike(s.Category, categoryPattern)));
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            var requestedLevel = level.Trim();

            usersQuery = usersQuery.Where(u =>
                _context.Skills.Any(s =>
                    s.UserId == u.Id &&
                    s.Level == requestedLevel));
        }

        usersQuery = usersQuery.Where(u =>
            _context.Skills.Any(s => s.UserId == u.Id));

        var users = await usersQuery
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Country
            })
            .ToListAsync();

        if (users.Count == 0)
        {
            return new List<TeacherSearchResponse>();
        }

        var userIds = users.Select(u => u.Id).ToList();

        var skills = await _context.Skills
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .OrderBy(s => s.Title)
            .Select(s => new
            {
                s.UserId,
                s.Title
            })
            .ToListAsync();

        var skillsByUser = skills
            .GroupBy(s => s.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(s => s.Title).ToList());

        return users.Select(user => new TeacherSearchResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Country = user.Country,
            Skills = skillsByUser.TryGetValue(user.Id, out var titles)
                ? titles
                : new List<string>(),
            SkillCount = skillsByUser.TryGetValue(user.Id, out var countTitles)
                ? countTitles.Count
                : 0
        }).ToList();
    }

    public async Task<TeacherDetailsResponse?> GetTeacherByIdAsync(
        string userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Country
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        var skills = await _context.Skills
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Title)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Category = s.Category,
                Level = s.Level,
                HourlyRate = s.HourlyRate,
                Currency = s.Currency,
                UserId = s.UserId,
                TeacherName = user.FullName,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();

        var availability = await _context.Availabilities
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                a.EndAtUtc > DateTime.UtcNow)
            .OrderBy(a => a.StartAtUtc)
            .Select(a => new AvailabilityResponse
            {
                Id = a.Id,
                UserId = a.UserId,
                StartAtUtc = a.StartAtUtc,
                EndAtUtc = a.EndAtUtc,
                CreatedAtUtc = a.CreatedAtUtc,
                UpdatedAtUtc = a.UpdatedAtUtc
            })
            .ToListAsync();

        return new TeacherDetailsResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Country = user.Country,
            Skills = skills,
            Availability = availability
        };
    }
}