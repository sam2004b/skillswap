
using Backend.Data;
using Backend.DTOs;
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
        string? search,
        string? category,
        string? country)
    {
        var query = _context.Skills
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
                s.Category == selectedCategory);
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var selectedCountry = country.Trim();

            query = query.Where(s =>
                s.Teacher != null &&
                s.Teacher.Country == selectedCountry);
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
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<List<TeacherSearchResponse>> SearchTeachersAsync(
        string? skill,
        string? category,
        string? country)
    {
        var skillsQuery = _context.Skills
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(skill))
        {
            var term = skill.Trim();

            skillsQuery = skillsQuery.Where(s =>
                s.Name.Contains(term) ||
                s.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var selectedCategory = category.Trim();

            skillsQuery = skillsQuery.Where(s =>
                s.Category == selectedCategory);
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var selectedCountry = country.Trim();

            skillsQuery = skillsQuery.Where(s =>
                s.Teacher != null &&
                s.Teacher.Country == selectedCountry);
        }

        var matchedSkills = await skillsQuery
            .Select(s => new
            {
                s.TeacherId,
                TeacherName = s.Teacher != null
                    ? s.Teacher.FullName
                    : string.Empty,
                Country = s.Teacher != null
                    ? s.Teacher.Country
                    : string.Empty,
                s.Name
            })
            .ToListAsync();

        return matchedSkills
            .GroupBy(s => new
            {
                s.TeacherId,
                s.TeacherName,
                s.Country
            })
            .Select(group => new TeacherSearchResponse
            {
                Id = group.Key.TeacherId,
                FullName = group.Key.TeacherName,
                Country = group.Key.Country,
                Skills = group.Select(s => s.Name)
                    .Distinct()
                    .ToList()
            })
            .OrderBy(t => t.FullName)
            .ToList();
    }

    public async Task<TeacherProfileResponse?> GetTeacherProfileAsync(
        string teacherId)
    {
        var teacher = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == teacherId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Country
            })
            .FirstOrDefaultAsync();

        if (teacher == null)
        {
            return null;
        }

        var skills = await _context.Skills
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId && s.IsActive)
            .Select(s => new SkillResponse
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Category = s.Category,
                TeacherId = s.TeacherId,
                TeacherName = teacher.FullName,
                HourlyRate = s.HourlyRate,
                Currency = s.Currency,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync();

        return new TeacherProfileResponse
        {
            Id = teacher.Id,
            FullName = teacher.FullName,
            Country = teacher.Country,
            Skills = skills
        };
    }
}
