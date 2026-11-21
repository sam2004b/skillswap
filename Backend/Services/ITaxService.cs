using Backend.DTOs;

namespace Backend.Services;

public interface ITaxService
{
    Task<TaxCalculationResponse> CalculateTaxAsync(
        int bookingId,
        string customerId,
        CancellationToken cancellationToken = default);

    Task SaveTaxProfileAsync(
        string userId,
        TaxProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<TaxProfileResponse?> GetTaxProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
