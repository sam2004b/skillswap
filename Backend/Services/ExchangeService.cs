using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ExchangeService : IExchangeService
{
    private readonly ApplicationDbContext _db;
    public ExchangeService(ApplicationDbContext db) => _db = db;

    public async Task<CreditWalletResponse> GetWalletAsync(string userId, CancellationToken ct = default)
    {
        var wallet = await EnsureWalletAsync(userId, ct);
        var transactions = await _db.CreditTransactions.AsNoTracking()
            .Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAtUtc).Take(100)
            .Select(t => new CreditTransactionResponse { Id=t.Id, Type=t.Type, Amount=t.Amount, BalanceAfter=t.BalanceAfter, BookingId=t.BookingId, Description=t.Description, CreatedAtUtc=t.CreatedAtUtc }).ToListAsync(ct);
        return new CreditWalletResponse { Balance=wallet.Balance, EscrowBalance=wallet.EscrowBalance, Transactions=transactions };
    }

    public async Task<object> CreateCreditBookingAsync(string userId, CreateCreditBookingRequest request, CancellationToken ct = default)
    {
        ValidateTime(request.StartAtUtc, request.EndAtUtc);
        if (request.StartAtUtc <= DateTime.UtcNow) throw new InvalidOperationException("Booking must be scheduled in the future.");
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var skill = await _db.Skills.FirstOrDefaultAsync(s => s.Id == request.SkillId, ct) ?? throw new KeyNotFoundException("Skill not found.");
        if (skill.UserId == userId) throw new InvalidOperationException("You cannot book your own skill.");
        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == skill.UserId && u.IsActive, ct) ?? throw new InvalidOperationException("Teacher is unavailable.");
        if (!await _db.Users.AnyAsync(u => u.Id == userId && u.IsActive, ct)) throw new KeyNotFoundException("User not found.");
        if (!await _db.Availabilities.AnyAsync(a => a.UserId == teacher.Id && a.StartAtUtc <= request.StartAtUtc && a.EndAtUtc >= request.EndAtUtc, ct)) throw new InvalidOperationException("Requested time is outside teacher availability.");
        if (await _db.Bookings.AnyAsync(b => (b.Status == "Pending" || b.Status == "Confirmed") && (b.StudentId == userId || b.TeacherId == userId || b.StudentId == teacher.Id || b.TeacherId == teacher.Id) && b.StartAtUtc < request.EndAtUtc && b.EndAtUtc > request.StartAtUtc, ct)) throw new InvalidOperationException("A participant already has a booking at that time.");
        var durationHours = (decimal)(request.EndAtUtc - request.StartAtUtc).TotalHours;
        var creditCost = Math.Round(skill.CreditRate * durationHours, 2, MidpointRounding.AwayFromZero);
        if (creditCost <= 0m) throw new InvalidOperationException("This skill has an invalid credit rate.");
        var learnerWallet = await EnsureWalletAsync(userId, ct);
        if (learnerWallet.Balance < creditCost) throw new InvalidOperationException("Insufficient credits.");
        var booking = new Booking { SkillId=skill.Id, StudentId=userId, TeacherId=teacher.Id, StartAtUtc=request.StartAtUtc, EndAtUtc=request.EndAtUtc, HourlyRate=skill.HourlyRate, TotalPrice=0m, Currency=skill.Currency, PaymentMethod="Credits", Status="Confirmed", CreditCost=creditCost, CreatedAtUtc=DateTime.UtcNow };
        _db.Bookings.Add(booking);
        learnerWallet.Balance -= creditCost; learnerWallet.EscrowBalance += creditCost; learnerWallet.UpdatedAtUtc=DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        _db.CreditTransactions.Add(new CreditTransaction { UserId=userId, Wallet=learnerWallet, Type="LessonEscrow", Amount=-creditCost, BalanceAfter=learnerWallet.Balance, BookingId=booking.Id, Description="Credits reserved for lesson" });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new { booking.Id, booking.SkillId, booking.StudentId, booking.TeacherId, booking.StartAtUtc, booking.EndAtUtc, booking.PaymentMethod, booking.CreditCost, booking.Status };
    }

    public async Task<object> CompleteCreditBookingAsync(string userId, int bookingId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct) ?? throw new KeyNotFoundException("Booking not found.");
        if (booking.PaymentMethod != "Credits") throw new InvalidOperationException("This is not a credit booking.");
        if (booking.TeacherId != userId) throw new UnauthorizedAccessException("Only the teacher can complete this lesson.");
        if (booking.Status != "Confirmed" || booking.EndAtUtc > DateTime.UtcNow) throw new InvalidOperationException("Only an ended, confirmed lesson can be completed.");
        var teacherWallet = await EnsureWalletAsync(booking.TeacherId, ct);
        var learnerWallet = await EnsureWalletAsync(booking.StudentId, ct);
        teacherWallet.Balance += booking.CreditCost; teacherWallet.UpdatedAtUtc=DateTime.UtcNow;
        learnerWallet.EscrowBalance -= booking.CreditCost; learnerWallet.UpdatedAtUtc=DateTime.UtcNow;
        booking.Status="Completed"; booking.UpdatedAtUtc=DateTime.UtcNow;
        _db.CreditTransactions.Add(new CreditTransaction { UserId=booking.TeacherId, Wallet=teacherWallet, Type="LessonEarned", Amount=booking.CreditCost, BalanceAfter=teacherWallet.Balance, BookingId=booking.Id, Description="Credits earned for completed lesson" });
        await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new { booking.Id, booking.Status, TeacherCreditReward=booking.CreditCost };
    }

    public async Task<object> CancelCreditBookingAsync(string userId, int bookingId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct) ?? throw new KeyNotFoundException("Booking not found.");
        if (booking.PaymentMethod != "Credits") throw new InvalidOperationException("This is not a credit booking.");
        if (booking.StudentId != userId && booking.TeacherId != userId) throw new UnauthorizedAccessException("You cannot cancel this booking.");
        if (booking.Status != "Confirmed") throw new InvalidOperationException("Only confirmed bookings can be cancelled.");
        var wallet = await EnsureWalletAsync(booking.StudentId, ct);
        wallet.Balance += booking.CreditCost; wallet.EscrowBalance -= booking.CreditCost; wallet.UpdatedAtUtc=DateTime.UtcNow;
        booking.Status="Cancelled"; booking.UpdatedAtUtc=DateTime.UtcNow;
        _db.CreditTransactions.Add(new CreditTransaction { UserId=booking.StudentId, Wallet=wallet, Type="LessonRefund", Amount=booking.CreditCost, BalanceAfter=wallet.Balance, BookingId=booking.Id, Description="Credits returned for cancelled lesson" });
        await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new { booking.Id, booking.Status, RefundedCredits=booking.CreditCost };
    }

    public async Task<SkillSwapResponse> CreateSkillSwapAsync(string userId, CreateSkillSwapRequest request, CancellationToken ct = default)
    {
        if (request.OfferedSkillId == request.RequestedSkillId) throw new InvalidOperationException("Choose two different skills.");
        var offered = await _db.Skills.FirstOrDefaultAsync(s => s.Id == request.OfferedSkillId, ct) ?? throw new KeyNotFoundException("Offered skill not found.");
        var requested = await _db.Skills.FirstOrDefaultAsync(s => s.Id == request.RequestedSkillId, ct) ?? throw new KeyNotFoundException("Requested skill not found.");
        if (offered.UserId != userId) throw new UnauthorizedAccessException("You must own the skill you offer.");
        if (requested.UserId == userId) throw new InvalidOperationException("You cannot swap with yourself.");
        var swap = new SkillSwapRequest { ProposerId=userId, RecipientId=requested.UserId, OfferedSkillId=offered.Id, RequestedSkillId=requested.Id, Message=request.Message?.Trim(), Status="Pending" };
        _db.SkillSwapRequests.Add(swap); await _db.SaveChangesAsync(ct); return await GetSwapAsync(swap.Id, ct);
    }

    public async Task<SkillSwapResponse> RespondToSkillSwapAsync(string userId, int id, bool accept, CancellationToken ct = default)
    {
        var swap = await _db.SkillSwapRequests.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new KeyNotFoundException("Skill swap request not found.");
        if (swap.RecipientId != userId) throw new UnauthorizedAccessException("Only the requested user can respond.");
        if (swap.Status != "Pending") throw new InvalidOperationException("This request has already been answered.");
        swap.Status=accept ? "Accepted" : "Rejected"; swap.RespondedAtUtc=DateTime.UtcNow; await _db.SaveChangesAsync(ct); return await GetSwapAsync(id, ct);
    }

    public async Task<SkillSwapResponse> CompleteSkillSwapAsync(string userId, int id, CancellationToken ct = default)
    {
        var swap = await _db.SkillSwapRequests.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new KeyNotFoundException("Skill swap request not found.");
        if (swap.ProposerId != userId && swap.RecipientId != userId) throw new UnauthorizedAccessException("You are not part of this swap.");
        if (swap.Status != "Accepted") throw new InvalidOperationException("Only accepted swaps can be completed.");
        if (swap.ProposerId == userId) swap.ProposerConfirmedCompletion = true;
        if (swap.RecipientId == userId) swap.RecipientConfirmedCompletion = true;
        if (swap.ProposerConfirmedCompletion && swap.RecipientConfirmedCompletion)
        {
            swap.Status = "Completed";
            swap.CompletedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return await GetSwapAsync(id, ct);
    }

    public async Task<List<SkillSwapResponse>> GetMySkillSwapsAsync(string userId, CancellationToken ct = default)
    {
        var ids = await _db.SkillSwapRequests.AsNoTracking().Where(s => s.ProposerId == userId || s.RecipientId == userId).OrderByDescending(s => s.CreatedAtUtc).Select(s => s.Id).ToListAsync(ct);
        var result = new List<SkillSwapResponse>(); foreach (var id in ids) result.Add(await GetSwapAsync(id, ct)); return result;
    }

    private async Task<CreditWallet> EnsureWalletAsync(string userId, CancellationToken ct)
    {
        var wallet = await _db.CreditWallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        if (wallet != null) return wallet;
        if (!await _db.Users.AnyAsync(u => u.Id == userId, ct)) throw new KeyNotFoundException("User not found.");
        wallet = new CreditWallet { UserId=userId, Balance=20m };
        _db.CreditWallets.Add(wallet); await _db.SaveChangesAsync(ct);
        _db.CreditTransactions.Add(new CreditTransaction { UserId=userId, Wallet=wallet, Type="WelcomeBonus", Amount=20m, BalanceAfter=20m, Description="Initial SkillSwap promotional credits" });
        await _db.SaveChangesAsync(ct); return wallet;
    }

    private Task<SkillSwapResponse> GetSwapAsync(int id, CancellationToken ct) => _db.SkillSwapRequests.AsNoTracking().Where(s => s.Id == id).Select(s => new SkillSwapResponse { Id=s.Id, ProposerId=s.ProposerId, ProposerName=s.Proposer!.FullName, RecipientId=s.RecipientId, RecipientName=s.Recipient!.FullName, OfferedSkillId=s.OfferedSkillId, OfferedSkillTitle=s.OfferedSkill!.Title, RequestedSkillId=s.RequestedSkillId, RequestedSkillTitle=s.RequestedSkill!.Title, Status=s.Status, Message=s.Message, CreatedAtUtc=s.CreatedAtUtc, RespondedAtUtc=s.RespondedAtUtc, CompletedAtUtc=s.CompletedAtUtc }).FirstAsync(ct);
    private static void ValidateTime(DateTime start, DateTime end) { if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc || end <= start) throw new InvalidOperationException("Provide valid UTC start and end times."); }
}
