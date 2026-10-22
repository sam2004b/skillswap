
using Backend.DTOs;

namespace Backend.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(
        CreateBookingRequest request, string learnerId);

    Task<List<BookingResponse>> GetMyBookingsAsync(string userId);

    Task<BookingResponse?> GetByIdAsync(int id, string userId);

    Task<BookingResponse?> RescheduleAsync(
        int id, RescheduleBookingRequest request, string userId);

    Task<BookingResponse?> CancelAsync(int id, string userId);

    Task<BookingResponse?> CompleteAsync(int id, string userId);
}
