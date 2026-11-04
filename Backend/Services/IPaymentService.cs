using Backend.DTOs;

namespace Backend.Services;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(
        string studentId,
        int bookingId);

    Task<List<PaymentResponse>> GetMyPaymentsAsync(
        string userId);

    Task<PaymentResponse?> GetPaymentByIdAsync(
        string userId,
        int paymentId);

    Task<PaymentResponse> RefundPaymentAsync(
        string userId,
        int paymentId);

    Task<StripeConnectResponse> CreateOrRefreshConnectOnboardingAsync(
        string teacherId);

    Task<StripeConnectResponse> GetConnectStatusAsync(
        string teacherId);

    Task ProcessWebhookAsync(
        string json,
        string signature);
}