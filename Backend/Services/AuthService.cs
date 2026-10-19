using System.Collections.Concurrent;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtService _jwtService;

    // Development-only token revocation store.
    private static readonly ConcurrentDictionary<string, DateTime>
        RevokedTokens = new();

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            AccountType = request.AccountType,
            Country = request.Country
        };

        var result = await _userManager.CreateAsync(
            user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ", result.Errors.Select(e => e.Description));

            throw new InvalidOperationException(errors);
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email);

        if (user is null)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
            return null;

        return CreateAuthResponse(user);
    }

    public Task<bool> LogoutAsync(string userId)
    {
        // A user ID alone cannot identify a particular JWT.
        // Token revocation should use the current token's unique ID.
        // This method therefore does not revoke a token by itself.
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(userId));
    }

    public async Task<AuthResponse?> GetProfileAsync(
        string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return null;

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return null;

        user.FullName = request.FullName;
        user.Country = request.Country;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ", result.Errors.Select(e => e.Description));

            throw new InvalidOperationException(errors);
        }

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(ApplicationUser user)
    {
        var (token, expiresAtUtc) =
            _jwtService.GenerateToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AccountType = user.AccountType,
            Country = user.Country,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
