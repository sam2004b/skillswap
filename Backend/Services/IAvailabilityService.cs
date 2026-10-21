using Backend.DTOs;
namespace Backend.Services;

public interface IAvailabilityService
{
    Task<AvailabilityResponse> CreateAsync(
        CreateAvailabilityRequest request,
        string userId);

    Task<List<AvailabilityResponse>> GetMyAvailabilityAsync(
        string userId);

    Task<List<AvailabilityResponse>> GetUserAvailabilityAsync(
        string userId);

    Task<AvailabilityResponse?> UpdateAsync(
        int id,
        UpdateAvailabilityRequest request,
        string userId);

    Task<bool> DeleteAsync(int id, string userId);
}
