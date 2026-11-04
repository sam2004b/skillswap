using Microsoft.AspNetCore.Identity;

namespace Backend.Models;

public class AuthenticationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string AccountType { get; set; } = "Individual";

    public string? Country { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public string? StripeAccountId { get; set; }

    public bool StripeOnboardingComplete { get; set; }

    public bool StripeChargesEnabled { get; set; }

    public bool StripePayoutsEnabled { get; set; }
}