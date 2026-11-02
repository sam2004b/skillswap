using Backend.DTOs;

namespace Backend.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task LogoutAsync(string userId);

    Task<AuthResponse?> GetProfileAsync(string userId);

    Task<AuthResponse?> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request);
}