using Backend.Data;
using Backend.DTOs.Bookings;
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

    public async Task<BookingResponse> CreateBookingAsync(
        string studentId,
        CreateBookingRequest request)
    {
        ValidateTime(request.StartAtUtc, request.EndAtUtc);

        if (request.StartAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "Booking must be scheduled in the future.");
        }

        var skill = await _context.Skills
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SkillId);

        if (skill == null)
        {
            throw new KeyNotFoundException("Skill not found.");
        }

        if (skill.UserId == studentId)
        {
            throw new InvalidOperationException(
                "You cannot book your own skill.");
        }

        var teacher = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == skill.UserId && u.IsActive);

        if (teacher == null)
        {
            throw new InvalidOperationException(
                "Teacher is not available.");
        }

        var studentExists = await _context.Users
            .AnyAsync(u => u.Id == studentId && u.IsActive);

        if (!studentExists)
        {
            throw new InvalidOperationException(
                "Student account is not available.");
        }

        await ValidateAvailabilityAsync(
            skill.UserId,
            request.StartAtUtc,
            request.EndAtUtc);

        await ValidateNoConflictAsync(
            studentId,
            skill.UserId,
            request.StartAtUtc,
            request.EndAtUtc,
            null);

        var durationHours =
            (decimal)(request.EndAtUtc - request.StartAtUtc)
                .TotalHours;

        var booking = new Booking
        {
            SkillId = skill.Id,
            StudentId = studentId,
            TeacherId = skill.UserId,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            HourlyRate = skill.HourlyRate,
            TotalPrice = Math.Round(
                skill.HourlyRate * durationHours, 2),
            Currency = skill.Currency,
            Status = "Confirmed",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);

        await _context.SaveChangesAsync();

        return await GetResponseAsync(booking.Id);
    }

    public async Task<List<BookingResponse>> GetMyBookingsAsync(
        string userId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.StudentId == userId ||
                b.TeacherId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                SkillId = b.SkillId,
                SkillTitle = b.Skill!.Title,
                StudentId = b.StudentId,
                StudentName = b.Student!.FullName,
                TeacherId = b.TeacherId,
                TeacherName = b.Teacher!.FullName,
                StartAtUtc = b.StartAtUtc,
                EndAtUtc = b.EndAtUtc,
                HourlyRate = b.HourlyRate,
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                Status = b.Status,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(
        int id,
        string userId)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Where(b =>
                b.Id == id &&
                (b.StudentId == userId || b.TeacherId == userId))
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                SkillId = b.SkillId,
                SkillTitle = b.Skill!.Title,
                StudentId = b.StudentId,
                StudentName = b.Student!.FullName,
                TeacherId = b.TeacherId,
                TeacherName = b.Teacher!.FullName,
                StartAtUtc = b.StartAtUtc,
                EndAtUtc = b.EndAtUtc,
                HourlyRate = b.HourlyRate,
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                Status = b.Status,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        return booking;
    }

    public async Task<BookingResponse> RescheduleBookingAsync(
        int id,
        string userId,
        RescheduleBookingRequest request)
    {
        ValidateTime(request.StartAtUtc, request.EndAtUtc);

        if (request.StartAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "Booking must be scheduled in the future.");
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.StudentId != userId &&
            booking.TeacherId != userId)
        {
            throw new UnauthorizedAccessException(
                "You cannot modify this booking.");
        }

        if (booking.Status != "Confirmed")
        {
            throw new InvalidOperationException(
                "Only confirmed bookings can be rescheduled.");
        }

        await ValidateAvailabilityAsync(
            booking.TeacherId,
            request.StartAtUtc,
            request.EndAtUtc);

        await ValidateNoConflictAsync(
            booking.StudentId,
            booking.TeacherId,
            request.StartAtUtc,
            request.EndAtUtc,
            booking.Id);

        booking.StartAtUtc = request.StartAtUtc;
        booking.EndAtUtc = request.EndAtUtc;

        var durationHours =
            (decimal)(request.EndAtUtc - request.StartAtUtc)
                .TotalHours;

        booking.TotalPrice = Math.Round(
            booking.HourlyRate * durationHours, 2);

        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetResponseAsync(booking.Id);
    }

    public async Task<BookingResponse> CancelBookingAsync(
        int id,
        string userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.StudentId != userId &&
            booking.TeacherId != userId)
        {
            throw new UnauthorizedAccessException(
                "You cannot cancel this booking.");
        }

        if (booking.Status != "Confirmed")
        {
            throw new InvalidOperationException(
                "Only confirmed bookings can be cancelled.");
        }

        booking.Status = "Cancelled";
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetResponseAsync(booking.Id);
    }

    public async Task<BookingResponse> CompleteBookingAsync(
        int id,
        string userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        if (booking.TeacherId != userId)
        {
            throw new UnauthorizedAccessException(
                "Only the teacher can complete this booking.");
        }

        if (booking.Status != "Confirmed")
        {
            throw new InvalidOperationException(
                "Only confirmed bookings can be completed.");
        }

        if (booking.EndAtUtc > DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "A booking cannot be completed before its end time.");
        }

        booking.Status = "Completed";
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetResponseAsync(booking.Id);
    }

    private async Task ValidateAvailabilityAsync(
        string teacherId,
        DateTime startAtUtc,
        DateTime endAtUtc)
    {
        var available = await _context.Availabilities
            .AnyAsync(a =>
                a.UserId == teacherId &&
                a.StartAtUtc <= startAtUtc &&
                a.EndAtUtc >= endAtUtc);

        if (!available)
        {
            throw new InvalidOperationException(
                "The requested time is outside the teacher's availability.");
        }
    }

    private async Task ValidateNoConflictAsync(
        string studentId,
        string teacherId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        int? excludedBookingId)
    {
        var conflict = await _context.Bookings
            .AnyAsync(b =>
                (!excludedBookingId.HasValue ||
                    b.Id != excludedBookingId.Value) &&
                (b.Status == "Pending" || b.Status == "Confirmed") &&
                (b.StudentId == studentId ||
                    b.TeacherId == teacherId) &&
                b.StartAtUtc < endAtUtc &&
                b.EndAtUtc > startAtUtc);

        if (conflict)
        {
            throw new InvalidOperationException(
                "The student or teacher already has a booking during this time.");
        }
    }

    private static void ValidateTime(
        DateTime startAtUtc,
        DateTime endAtUtc)
    {
        if (endAtUtc <= startAtUtc)
        {
            throw new InvalidOperationException(
                "End time must be later than start time.");
        }

        if (startAtUtc.Kind != DateTimeKind.Utc ||
            endAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "Booking times must be provided in UTC.");
        }
    }

    private async Task<BookingResponse> GetResponseAsync(int id)
    {
        var response = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookingResponse
            {
                Id = b.Id,
                SkillId = b.SkillId,
                SkillTitle = b.Skill!.Title,
                StudentId = b.StudentId,
                StudentName = b.Student!.FullName,
                TeacherId = b.TeacherId,
                TeacherName = b.Teacher!.FullName,
                StartAtUtc = b.StartAtUtc,
                EndAtUtc = b.EndAtUtc,
                HourlyRate = b.HourlyRate,
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                Status = b.Status,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (response == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        return response;
    }
}