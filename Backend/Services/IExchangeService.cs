
using Backend.DTOs;

namespace Backend.Services;

public interface IExchangeService
{
    Task<CreditWalletResponse> GetWalletAsync(
        string userId,
        CancellationToken ct = default);

    Task<object> CreateCreditBookingAsync(
        string userId,
        CreateCreditBookingRequest request,
        CancellationToken ct = default);

    Task<object> CompleteCreditBookingAsync(
        string userId,
        int bookingId,
        CancellationToken ct = default);

    Task<object> CancelCreditBookingAsync(
        string userId,
        int bookingId,
        CancellationToken ct = default);

    Task<SkillSwapResponse> CreateSkillSwapAsync(
        string userId,
        CreateSkillSwapRequest request,
        CancellationToken ct = default);

    Task<SkillSwapResponse> RespondToSkillSwapAsync(
        string userId,
        int id,
        bool accept,
        CancellationToken ct = default);

    Task<SkillSwapResponse> CompleteSkillSwapAsync(
        string userId,
        int id,
        CancellationToken ct = default);

    Task<List<SkillSwapResponse>> GetMySkillSwapsAsync(
        string userId,
        CancellationToken ct = default);
}
