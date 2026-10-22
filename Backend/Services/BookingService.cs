
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookingResponse> CreateAsync(
        CreateBookingRequest request, string learnerId)
    {
        var start = request.StartAtUtc.UtcDateTime;
        var end = request.EndAtUtc.UtcDateTime;

        ValidateTimes(start, end);

        var skill = await _context.Skills
            .FirstOrDefaultAsync(s =>
                s.Id == request.SkillId && s.IsActive);

        if (skill == null)
            throw new ArgumentException("Skill not found or inactive.");

        var teacherId = skill.TeacherId;

        if (teacherId == learnerId)
            throw new ArgumentException(
                "You cannot book your own skill.");

        var teacherExists = await _context.Users
            .AnyAsync(u => u.Id == teacherId);

        if (!teacherExists)
            throw new ArgumentException("Teacher not found.");

        var hasAvailability = await _context.Availabilities
            .AnyAsync(a =>
                a.UserId == teacherId &&
                a.StartAtUtc <= start &&
                a.EndAtUtc >= end);

        if (!hasAvailability)
            throw new ArgumentException(
                "The requested time is outside the teacher's availability.");

        await EnsureNoOverlapAsync(teacherId, start, end, null);
        await EnsureNoOverlapAsync(learnerId, start, end, null);

        var booking = new Booking
        {
            LearnerId = learnerId,
            TeacherId = teacherId,
            SkillId = skill.Id,
            StartAtUtc = start,
            EndAtUtc = end,
            BookingType = request.BookingType,
            Status = "Confirmed",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return await BuildResponseAsync(booking.Id);
    }

    public async Task<List<BookingResponse>> GetMyBookingsAsync(
        string userId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.LearnerId == userId ||
                b.TeacherId == userId)
            .OrderByDescending(b => b.StartAtUtc)
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                LearnerId = b.LearnerId,
                LearnerName = b.Learner != null
                    ? b.Learner.FullName : "",
                TeacherId = b.TeacherId,
                TeacherName = b.Teacher != null
                    ? b.Teacher.FullName : "",
                SkillId = b.SkillId,
                SkillName = b.Skill != null
                    ? b.Skill.Name : "",
                StartAtUtc = b.StartAtUtc,
                EndAtUtc = b.EndAtUtc,
                Status = b.Status,
                BookingType = b.BookingType,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<BookingResponse?> GetByIdAsync(
        int id, string userId)
    {
        var exists = await _context.Bookings
            .AnyAsync(b =>
                b.Id == id &&
                (b.LearnerId == userId ||
                 b.TeacherId == userId));

        if (!exists)
            return null;

        return await BuildResponseAsync(id);
    }

    public async Task<BookingResponse?> RescheduleAsync(
        int id, RescheduleBookingRequest request, string userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                (b.LearnerId == userId ||
                 b.TeacherId == userId));

        if (booking == null)
            return null;

        if (booking.Status != "Confirmed")
            throw new ArgumentException(
                "Only confirmed bookings can be rescheduled.");

        if (booking.StartAtUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "A booking that has already started cannot be rescheduled.");

        var start = request.StartAtUtc.UtcDateTime;
        var end = request.EndAtUtc.UtcDateTime;

        ValidateTimes(start, end);

        var hasAvailability = await _context.Availabilities
            .AnyAsync(a =>
                a.UserId == booking.TeacherId &&
                a.StartAtUtc <= start &&
                a.EndAtUtc >= end);

        if (!hasAvailability)
            throw new ArgumentException(
                "The new time is outside the teacher's availability.");

        await EnsureNoOverlapAsync(
            booking.TeacherId, start, end, id);

        await EnsureNoOverlapAsync(
            booking.LearnerId, start, end, id);

        booking.StartAtUtc = start;
        booking.EndAtUtc = end;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await BuildResponseAsync(id);
    }

    public async Task<BookingResponse?> CancelAsync(
        int id, string userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                (b.LearnerId == userId ||
                 b.TeacherId == userId));

        if (booking == null)
            return null;

        if (booking.Status != "Confirmed")
            throw new ArgumentException(
                "Only confirmed bookings can be cancelled.");

        if (booking.StartAtUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "A booking that has already started cannot be cancelled.");

        booking.Status = "Cancelled";
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await BuildResponseAsync(id);
    }

    public async Task<BookingResponse?> CompleteAsync(
        int id, string userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                (b.LearnerId == userId ||
                 b.TeacherId == userId));

        if (booking == null)
            return null;

        if (booking.Status != "Confirmed")
            throw new ArgumentException(
                "Only confirmed bookings can be completed.");

        if (booking.EndAtUtc > DateTime.UtcNow)
            throw new ArgumentException(
                "A booking cannot be completed before its end time.");

        booking.Status = "Completed";
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await BuildResponseAsync(id);
    }

    private void ValidateTimes(DateTime start, DateTime end)
    {
        if (start <= DateTime.UtcNow)
            throw new ArgumentException(
                "Booking start time must be in the future.");

        if (end <= start)
            throw new ArgumentException(
                "End time must be later than start time.");
    }

    private async Task EnsureNoOverlapAsync(
        string userId, DateTime start, DateTime end, int? excludeId)
    {
        var overlaps = await _context.Bookings.AnyAsync(b =>
            (b.LearnerId == userId || b.TeacherId == userId) &&
            b.Status == "Confirmed" &&
            (!excludeId.HasValue || b.Id != excludeId.Value) &&
            b.StartAtUtc < end &&
            b.EndAtUtc > start);

        if (overlaps)
            throw new ArgumentException(
                "This time conflicts with another confirmed booking.");
    }

    private async Task<BookingResponse> BuildResponseAsync(int id)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                LearnerId = b.LearnerId,
                LearnerName = b.Learner != null
                    ? b.Learner.FullName : "",
                TeacherId = b.TeacherId,
                TeacherName = b.Teacher != null
                    ? b.Teacher.FullName : "",
                SkillId = b.SkillId,
                SkillName = b.Skill != null
                    ? b.Skill.Name : "",
                StartAtUtc = b.StartAtUtc,
                EndAtUtc = b.EndAtUtc,
                Status = b.Status,
                BookingType = b.BookingType,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            })
            .FirstAsync();
    }
}
