using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AuthenticationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ApplicationDbContext _db;

    public AuthService(
        UserManager<AuthenticationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _db = db;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        string accountType = request.AccountType.Trim();

        if (accountType != "Individual" && accountType != "Business")
            throw new ArgumentException(
                "Account type must be Individual or Business.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
            throw new ArgumentException("Email is already registered.");

        var user = new AuthenticationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            AccountType = accountType,
            Country = request.Country,
            PhoneNumber = request.PhoneNumber,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            throw new ArgumentException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (!await _roleManager.RoleExistsAsync(accountType))
        {
            var roleResult = await _roleManager.CreateAsync(
                new IdentityRole(accountType));

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException("Unable to create role.");
            }
        }

        var assignResult = await _userManager.AddToRoleAsync(user, accountType);

        if (!assignResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new InvalidOperationException("Unable to assign role.");
        }

        var wallet = new CreditWallet { UserId = user.Id, Balance = 20m };
        _db.CreditWallets.Add(wallet);
        await _db.SaveChangesAsync();
        _db.CreditTransactions.Add(new CreditTransaction
        {
            UserId = user.Id,
            Wallet = wallet,
            Type = "WelcomeBonus",
            Amount = 20m,
            BalanceAfter = 20m,
            Description = "Initial SkillSwap promotional credits"
        });
        await _db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);

        return CreateAuthResponse(user, roles);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var passwordValid = await _userManager.CheckPasswordAsync(
            user, request.Password);

        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);

        return CreateAuthResponse(user, roles);
    }

    public async Task LogoutAsync(string userId)
    {
        await Task.CompletedTask;
    }

    public async Task<AuthResponse?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);

        return CreateAuthResponse(user, roles);
    }

    public async Task<AuthResponse?> UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return null;

        user.FullName = request.FullName;
        user.Country = request.Country;
        user.PhoneNumber = request.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new ArgumentException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var roles = await _userManager.GetRolesAsync(user);

        return CreateAuthResponse(user, roles);
    }

    private AuthResponse CreateAuthResponse(
        AuthenticationUser user,
        IList<string> roles)
    {
        var (token, expiresAt) = _jwtService.GenerateToken(user, roles);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AccountType = user.AccountType,
            Token = token,
            ExpiresAtUtc = expiresAt
        };
    }
}