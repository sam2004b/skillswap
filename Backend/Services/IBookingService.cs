using Backend.DTOs.Bookings;

namespace Backend.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(
        string studentId,
        CreateBookingRequest request);

    Task<List<BookingResponse>> GetMyBookingsAsync(string userId);

    Task<BookingResponse?> GetBookingByIdAsync(
        int id,
        string userId);

    Task<BookingResponse> RescheduleBookingAsync(
        int id,
        string userId,
        RescheduleBookingRequest request);

    Task<BookingResponse> CancelBookingAsync(
        int id,
        string userId);

    Task<BookingResponse> CompleteBookingAsync(
        int id,
        string userId);
}