
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class TaxService : ITaxService
{
    private readonly ApplicationDbContext _db;

    public TaxService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TaxCalculationResponse> CalculateTaxAsync(
        int bookingId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Student)
            .Include(b => b.Teacher)
            .FirstOrDefaultAsync(
                b => b.Id == bookingId &&
                     b.StudentId == customerId,
                cancellationToken);

        if (booking == null)
            throw new KeyNotFoundException("Booking not found.");

        if (booking.Student == null || booking.Teacher == null)
            throw new InvalidOperationException(
                "Booking user information is incomplete.");

        var studentProfile = await _db.TaxProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == booking.StudentId,
                cancellationToken);

        var teacherProfile = await _db.TaxProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == booking.TeacherId,
                cancellationToken);

        string customerCountry =
            studentProfile?.CountryCode ??
            booking.Student.Country ??
            string.Empty;

        string teacherCountry =
            teacherProfile?.CountryCode ??
            booking.Teacher.Country ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(customerCountry) ||
            string.IsNullOrWhiteSpace(teacherCountry))
        {
            throw new InvalidOperationException(
                "Learner and teacher country information is required for tax assessment.");
        }

        // Initial placeholder.
        // Replace this with verified tax rules or a tax provider.
        const decimal taxRate = 0m;

        decimal serviceAmount = booking.TotalPrice;

        decimal taxAmount = Math.Round(
            serviceAmount * taxRate / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return new TaxCalculationResponse
        {
            BookingId = booking.Id,
            ServiceAmount = serviceAmount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TotalAmount = serviceAmount + taxAmount,
            Currency = booking.Currency,
            CustomerCountry = customerCountry.ToUpperInvariant(),
            TeacherCountry = teacherCountry.ToUpperInvariant(),
            TaxJurisdiction = customerCountry.ToUpperInvariant(),
            TaxStatus = "PendingTaxRules"
        };
    }

    public async Task SaveTaxProfileAsync(
        string userId,
        TaxProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.TaxProfiles
            .FirstOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);

        if (profile == null)
        {
            profile = new TaxProfile
            {
                UserId = userId
            };

            _db.TaxProfiles.Add(profile);
        }

        profile.CountryCode =
            request.CountryCode.Trim().ToUpperInvariant();

        profile.TaxIdentificationNumber =
            request.TaxIdentificationNumber;

        profile.CustomerType = request.CustomerType;
        profile.IsBusinessRegistered =
            request.IsBusinessRegistered;

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaxProfileResponse?> GetTaxProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.TaxProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == userId,
                cancellationToken);

        if (profile == null)
            return null;

        return new TaxProfileResponse
        {
            CountryCode = profile.CountryCode,
            TaxIdentificationNumber =
                profile.TaxIdentificationNumber,
            CustomerType = profile.CustomerType,
            IsBusinessRegistered =
                profile.IsBusinessRegistered
        };
    }
}
