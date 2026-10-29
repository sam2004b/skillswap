using Backend.DTOs;

namespace Backend.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateCheckoutAsync(
        CreatePaymentRequest request,
        string userId);

    Task<List<PaymentResponse>> GetMyPaymentsAsync(
        string userId);

    Task<PaymentResponse?> GetPaymentAsync(
        int id,
        string userId);
}
