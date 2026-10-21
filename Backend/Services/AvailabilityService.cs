using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;

    public AvailabilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AvailabilityResponse> CreateAsync(
        CreateAvailabilityRequest request,
        string userId)
    {
        var (start, end) = ValidateTimes(
            request.StartAtUtc,
            request.EndAtUtc);

        await EnsureNoOverlapAsync(userId, start, end);

        var availability = new Availability
        {
            UserId = userId,
            StartAtUtc = start,
            EndAtUtc = end,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Availabilities.Add(availability);
        await _context.SaveChangesAsync();

        return ToResponse(availability);
    }

    public async Task<List<AvailabilityResponse>> GetMyAvailabilityAsync(
        string userId)
    {
        var slots = await _context.Availabilities
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.StartAtUtc)
            .ToListAsync();

        return slots.Select(ToResponse).ToList();
    }

    public async Task<List<AvailabilityResponse>> GetUserAvailabilityAsync(
        string userId)
    {
        var slots = await _context.Availabilities
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                a.EndAtUtc > DateTime.UtcNow)
            .OrderBy(a => a.StartAtUtc)
            .ToListAsync();

        return slots.Select(ToResponse).ToList();
    }

    public async Task<AvailabilityResponse?> UpdateAsync(
        int id,
        UpdateAvailabilityRequest request,
        string userId)
    {
        var availability = await _context.Availabilities
            .FirstOrDefaultAsync(a =>
                a.Id == id && a.UserId == userId);

        if (availability == null)
        {
            return null;
        }

        var (start, end) = ValidateTimes(
            request.StartAtUtc,
            request.EndAtUtc);

        await EnsureNoOverlapAsync(userId, start, end, id);

        availability.StartAtUtc = start;
        availability.EndAtUtc = end;
        availability.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToResponse(availability);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var availability = await _context.Availabilities
            .FirstOrDefaultAsync(a =>
                a.Id == id && a.UserId == userId);

        if (availability == null)
        {
            return false;
        }

        _context.Availabilities.Remove(availability);
        await _context.SaveChangesAsync();

        return true;
    }

    private static (DateTime Start, DateTime End) ValidateTimes(
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc)
    {
        var start = startAtUtc.UtcDateTime;
        var end = endAtUtc.UtcDateTime;

        if (start <= DateTime.UtcNow)
        {
            throw new ArgumentException(
                "Availability must start in the future.");
        }

        if (end <= start)
        {
            throw new ArgumentException(
                "End time must be later than start time.");
        }

        return (start, end);
    }

    private async Task EnsureNoOverlapAsync(
        string userId,
        DateTime start,
        DateTime end,
        int? excludeId = null)
    {
        var query = _context.Availabilities
            .Where(a =>
                a.UserId == userId &&
                a.StartAtUtc < end &&
                a.EndAtUtc > start);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        if (await query.AnyAsync())
        {
            throw new ArgumentException(
                "This availability overlaps an existing time slot.");
        }
    }

    private static AvailabilityResponse ToResponse(
        Availability availability)
    {
        return new AvailabilityResponse
        {
            Id = availability.Id,
            UserId = availability.UserId,
            StartAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    availability.StartAtUtc,
                    DateTimeKind.Utc)),
            EndAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    availability.EndAtUtc,
                    DateTimeKind.Utc)),
            CreatedAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    availability.CreatedAtUtc,
                    DateTimeKind.Utc)),
            UpdatedAtUtc = availability.UpdatedAtUtc.HasValue
                ? new DateTimeOffset(
                    DateTime.SpecifyKind(
                        availability.UpdatedAtUtc.Value,
                        DateTimeKind.Utc))
                : null
        };
    }
}
