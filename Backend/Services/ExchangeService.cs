
using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ExchangeService : IExchangeService
{
    private const decimal InitialCreditBalance = 20m;
    private const int TransactionHistoryLimit = 100;

    private readonly ApplicationDbContext _db;
    private readonly IUserNotificationService _notifications;

    public ExchangeService(
        ApplicationDbContext db,
        IUserNotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // CREDIT WALLET

    public async Task<CreditWalletResponse> GetWalletAsync(
        string userId,
        CancellationToken ct = default)
    {
        var wallet = await EnsureWalletAsync(userId, ct);

        var transactions = await _db.CreditTransactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Take(TransactionHistoryLimit)
            .Select(transaction => new CreditTransactionResponse
            {
                Id = transaction.Id,
                Type = transaction.Type,
                Amount = transaction.Amount,
                BalanceAfter = transaction.BalanceAfter,
                BookingId = transaction.BookingId,
                Description = transaction.Description,
                CreatedAtUtc = transaction.CreatedAtUtc
            })
            .ToListAsync(ct);

        return new CreditWalletResponse
        {
            Balance = wallet.Balance,
            EscrowBalance = wallet.EscrowBalance,
            Transactions = transactions
        };
    }

    // CREDIT BOOKINGS

    public async Task<object> CreateCreditBookingAsync(
        string userId,
        CreateCreditBookingRequest request,
        CancellationToken ct = default)
    {
        ValidateTime(request.StartAtUtc, request.EndAtUtc);

        if (request.StartAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "Booking must be scheduled in the future.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var skill = await _db.Skills
            .FirstOrDefaultAsync(
                item => item.Id == request.SkillId,
                ct)
            ?? throw new KeyNotFoundException("Skill not found.");

        if (skill.UserId == userId)
        {
            throw new InvalidOperationException(
                "You cannot book your own skill.");
        }

        var teacher = await _db.Users
            .FirstOrDefaultAsync(
                user => user.Id == skill.UserId && user.IsActive,
                ct)
            ?? throw new InvalidOperationException(
                "Teacher is unavailable.");

        var learnerExists = await _db.Users
            .AnyAsync(
                user => user.Id == userId && user.IsActive,
                ct);

        if (!learnerExists)
        {
            throw new KeyNotFoundException("User not found.");
        }

        await ValidateAvailabilityAsync(
            teacher.Id,
            userId,
            request.StartAtUtc,
            request.EndAtUtc,
            ct);

        var durationHours =
            (decimal)(request.EndAtUtc - request.StartAtUtc).TotalHours;

        var creditCost = Math.Round(
            skill.CreditRate * durationHours,
            2,
            MidpointRounding.AwayFromZero);

        if (creditCost <= 0m)
        {
            throw new InvalidOperationException(
                "This skill has an invalid credit rate.");
        }

        var learnerWallet = await EnsureWalletAsync(userId, ct);

        if (learnerWallet.Balance < creditCost)
        {
            throw new InvalidOperationException(
                "Insufficient credits.");
        }

        var now = DateTime.UtcNow;

        var booking = new Booking
        {
            SkillId = skill.Id,
            StudentId = userId,
            TeacherId = teacher.Id,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            HourlyRate = skill.HourlyRate,
            TotalPrice = 0m,
            Currency = skill.Currency,
            PaymentMethod = "Credits",
            Status = "Confirmed",
            CreditCost = creditCost,
            CreatedAtUtc = now
        };

        _db.Bookings.Add(booking);

        learnerWallet.Balance -= creditCost;
        learnerWallet.EscrowBalance += creditCost;
        learnerWallet.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(ct);

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Wallet = learnerWallet,
            Type = "LessonEscrow",
            Amount = -creditCost,
            BalanceAfter = learnerWallet.Balance,
            BookingId = booking.Id,
            Description = "Credits reserved for lesson"
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await _notifications.NotifyAsync(
            booking.TeacherId,
            "CreditBookingCreated",
            "New credit lesson",
            "A learner booked your skill using credits.",
            "Booking",
            booking.Id.ToString(),
            ct);

        return new
        {
            booking.Id,
            booking.SkillId,
            booking.StudentId,
            booking.TeacherId,
            booking.StartAtUtc,
            booking.EndAtUtc,
            booking.PaymentMethod,
            booking.CreditCost,
            booking.Status
        };
    }

    public async Task<object> CompleteCreditBookingAsync(
        string userId,
        int bookingId,
        CancellationToken ct = default)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var booking = await GetCreditBookingAsync(bookingId, ct);

        if (booking.TeacherId != userId)
        {
            throw new UnauthorizedAccessException(
                "Only the teacher can complete this lesson.");
        }

        if (booking.Status != "Confirmed" ||
            booking.EndAtUtc > DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "Only an ended, confirmed lesson can be completed.");
        }

        var teacherWallet = await EnsureWalletAsync(
            booking.TeacherId,
            ct);

        var learnerWallet = await EnsureWalletAsync(
            booking.StudentId,
            ct);

        var now = DateTime.UtcNow;

        teacherWallet.Balance += booking.CreditCost;
        teacherWallet.UpdatedAtUtc = now;

        learnerWallet.EscrowBalance -= booking.CreditCost;
        learnerWallet.UpdatedAtUtc = now;

        booking.Status = "Completed";
        booking.UpdatedAtUtc = now;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = booking.TeacherId,
            Wallet = teacherWallet,
            Type = "LessonEarned",
            Amount = booking.CreditCost,
            BalanceAfter = teacherWallet.Balance,
            BookingId = booking.Id,
            Description = "Credits earned for completed lesson"
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new
        {
            booking.Id,
            booking.Status,
            TeacherCreditReward = booking.CreditCost
        };
    }

    public async Task<object> CancelCreditBookingAsync(
        string userId,
        int bookingId,
        CancellationToken ct = default)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        var booking = await GetCreditBookingAsync(bookingId, ct);

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

        var learnerWallet = await EnsureWalletAsync(
            booking.StudentId,
            ct);

        var now = DateTime.UtcNow;

        learnerWallet.Balance += booking.CreditCost;
        learnerWallet.EscrowBalance -= booking.CreditCost;
        learnerWallet.UpdatedAtUtc = now;

        booking.Status = "Cancelled";
        booking.UpdatedAtUtc = now;

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = booking.StudentId,
            Wallet = learnerWallet,
            Type = "LessonRefund",
            Amount = booking.CreditCost,
            BalanceAfter = learnerWallet.Balance,
            BookingId = booking.Id,
            Description = "Credits returned for cancelled lesson"
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new
        {
            booking.Id,
            booking.Status,
            RefundedCredits = booking.CreditCost
        };
    }

    // SKILL SWAPS

    public async Task<SkillSwapResponse> CreateSkillSwapAsync(
        string userId,
        CreateSkillSwapRequest request,
        CancellationToken ct = default)
    {
        if (request.OfferedSkillId == request.RequestedSkillId)
        {
            throw new InvalidOperationException(
                "Choose two different skills.");
        }

        var offeredSkill = await _db.Skills
            .FirstOrDefaultAsync(
                skill => skill.Id == request.OfferedSkillId,
                ct)
            ?? throw new KeyNotFoundException(
                "Offered skill not found.");

        var requestedSkill = await _db.Skills
            .FirstOrDefaultAsync(
                skill => skill.Id == request.RequestedSkillId,
                ct)
            ?? throw new KeyNotFoundException(
                "Requested skill not found.");

        if (offeredSkill.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You must own the skill you offer.");
        }

        if (requestedSkill.UserId == userId)
        {
            throw new InvalidOperationException(
                "You cannot swap with yourself.");
        }

        var recipientExists = await _db.Users
            .AnyAsync(
                user => user.Id == requestedSkill.UserId &&
                        user.IsActive,
                ct);

        if (!recipientExists)
        {
            throw new InvalidOperationException(
                "The requested user is unavailable.");
        }

        var swap = new SkillSwapRequest
        {
            ProposerId = userId,
            RecipientId = requestedSkill.UserId,
            OfferedSkillId = offeredSkill.Id,
            RequestedSkillId = requestedSkill.Id,
            Message = request.Message?.Trim(),
            Status = "Pending"
        };

        _db.SkillSwapRequests.Add(swap);

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            swap.RecipientId,
            "SkillSwapRequested",
            "New skill-swap request",
            "Someone proposed exchanging skills with you.",
            "SkillSwap",
            swap.Id.ToString(),
            ct);

        return await GetSwapAsync(swap.Id, ct);
    }

    public async Task<SkillSwapResponse> RespondToSkillSwapAsync(
        string userId,
        int id,
        bool accept,
        CancellationToken ct = default)
    {
        var swap = await _db.SkillSwapRequests
            .FirstOrDefaultAsync(
                item => item.Id == id,
                ct)
            ?? throw new KeyNotFoundException(
                "Skill swap request not found.");

        if (swap.RecipientId != userId)
        {
            throw new UnauthorizedAccessException(
                "Only the requested user can respond.");
        }

        if (swap.Status != "Pending")
        {
            throw new InvalidOperationException(
                "This request has already been answered.");
        }

        swap.Status = accept ? "Accepted" : "Rejected";
        swap.RespondedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            swap.ProposerId,
            accept ? "SkillSwapAccepted" : "SkillSwapRejected",
            accept ? "Skill swap accepted" : "Skill swap declined",
            accept
                ? "Your skill-swap request was accepted."
                : "Your skill-swap request was declined.",
            "SkillSwap",
            swap.Id.ToString(),
            ct);

        return await GetSwapAsync(id, ct);
    }

    public async Task<SkillSwapResponse> CompleteSkillSwapAsync(
        string userId,
        int id,
        CancellationToken ct = default)
    {
        var swap = await _db.SkillSwapRequests
            .FirstOrDefaultAsync(
                item => item.Id == id,
                ct)
            ?? throw new KeyNotFoundException(
                "Skill swap request not found.");

        if (swap.ProposerId != userId &&
            swap.RecipientId != userId)
        {
            throw new UnauthorizedAccessException(
                "You are not part of this swap.");
        }

        if (swap.Status != "Accepted")
        {
            throw new InvalidOperationException(
                "Only accepted swaps can be completed.");
        }

        if (swap.ProposerId == userId)
        {
            swap.ProposerConfirmedCompletion = true;
        }
        else
        {
            swap.RecipientConfirmedCompletion = true;
        }

        if (swap.ProposerConfirmedCompletion &&
            swap.RecipientConfirmedCompletion)
        {
            swap.Status = "Completed";
            swap.CompletedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        var otherUserId = swap.ProposerId == userId
            ? swap.RecipientId
            : swap.ProposerId;

        var completed = swap.Status == "Completed";

        await _notifications.NotifyAsync(
            otherUserId,
            "SkillSwapProgress",
            "Skill-swap updated",
            completed
                ? "Both users confirmed completion of the skill swap."
                : "The other participant confirmed their part of the skill swap.",
            "SkillSwap",
            swap.Id.ToString(),
            ct);

        return await GetSwapAsync(id, ct);
    }

    public async Task<List<SkillSwapResponse>> GetMySkillSwapsAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await _db.SkillSwapRequests
            .AsNoTracking()
            .Where(swap =>
                swap.ProposerId == userId ||
                swap.RecipientId == userId)
            .OrderByDescending(swap => swap.CreatedAtUtc)
            .Select(swap => new SkillSwapResponse
            {
                Id = swap.Id,
                ProposerId = swap.ProposerId,
                ProposerName = swap.Proposer!.FullName,
                RecipientId = swap.RecipientId,
                RecipientName = swap.Recipient!.FullName,
                OfferedSkillId = swap.OfferedSkillId,
                OfferedSkillTitle = swap.OfferedSkill!.Title,
                RequestedSkillId = swap.RequestedSkillId,
                RequestedSkillTitle = swap.RequestedSkill!.Title,
                Status = swap.Status,
                Message = swap.Message,
                CreatedAtUtc = swap.CreatedAtUtc,
                RespondedAtUtc = swap.RespondedAtUtc,
                CompletedAtUtc = swap.CompletedAtUtc
            })
            .ToListAsync(ct);
    }

    // PRIVATE HELPERS

    private async Task<CreditWallet> EnsureWalletAsync(
        string userId,
        CancellationToken ct)
    {
        var wallet = await _db.CreditWallets
            .FirstOrDefaultAsync(
                item => item.UserId == userId,
                ct);

        if (wallet != null)
        {
            return wallet;
        }

        var userExists = await _db.Users
            .AnyAsync(user => user.Id == userId, ct);

        if (!userExists)
        {
            throw new KeyNotFoundException("User not found.");
        }

        wallet = new CreditWallet
        {
            UserId = userId,
            Balance = InitialCreditBalance
        };

        _db.CreditWallets.Add(wallet);

        await _db.SaveChangesAsync(ct);

        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Wallet = wallet,
            Type = "WelcomeBonus",
            Amount = InitialCreditBalance,
            BalanceAfter = InitialCreditBalance,
            Description = "Initial SkillSwap promotional credits"
        });

        await _db.SaveChangesAsync(ct);

        return wallet;
    }

    private async Task<Booking> GetCreditBookingAsync(
        int bookingId,
        CancellationToken ct)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(
                item => item.Id == bookingId,
                ct)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.PaymentMethod != "Credits")
        {
            throw new InvalidOperationException(
                "This is not a credit booking.");
        }

        return booking;
    }

    private async Task ValidateAvailabilityAsync(
        string teacherId,
        string learnerId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        CancellationToken ct)
    {
        var isAvailable = await _db.Availabilities
            .AnyAsync(
                availability =>
                    availability.UserId == teacherId &&
                    availability.StartAtUtc <= startAtUtc &&
                    availability.EndAtUtc >= endAtUtc,
                ct);

        if (!isAvailable)
        {
            throw new InvalidOperationException(
                "Requested time is outside teacher availability.");
        }

        var hasConflict = await _db.Bookings
            .AnyAsync(
                booking =>
                    (booking.Status == "Pending" ||
                     booking.Status == "Confirmed") &&
                    (
                        booking.StudentId == learnerId ||
                        booking.TeacherId == learnerId ||
                        booking.StudentId == teacherId ||
                        booking.TeacherId == teacherId
                    ) &&
                    booking.StartAtUtc < endAtUtc &&
                    booking.EndAtUtc > startAtUtc,
                ct);

        if (hasConflict)
        {
            throw new InvalidOperationException(
                "A participant already has a booking at that time.");
        }
    }

    private async Task<SkillSwapResponse> GetSwapAsync(
        int id,
        CancellationToken ct)
    {
        return await _db.SkillSwapRequests
            .AsNoTracking()
            .Where(swap => swap.Id == id)
            .Select(swap => new SkillSwapResponse
            {
                Id = swap.Id,
                ProposerId = swap.ProposerId,
                ProposerName = swap.Proposer!.FullName,
                RecipientId = swap.RecipientId,
                RecipientName = swap.Recipient!.FullName,
                OfferedSkillId = swap.OfferedSkillId,
                OfferedSkillTitle = swap.OfferedSkill!.Title,
                RequestedSkillId = swap.RequestedSkillId,
                RequestedSkillTitle = swap.RequestedSkill!.Title,
                Status = swap.Status,
                Message = swap.Message,
                CreatedAtUtc = swap.CreatedAtUtc,
                RespondedAtUtc = swap.RespondedAtUtc,
                CompletedAtUtc = swap.CompletedAtUtc
            })
            .FirstAsync(ct);
    }

    private static void ValidateTime(
        DateTime start,
        DateTime end)
    {
        if (start.Kind != DateTimeKind.Utc ||
            end.Kind != DateTimeKind.Utc ||
            end <= start)
        {
            throw new InvalidOperationException(
                "Provide valid UTC start and end times.");
        }
    }
}
