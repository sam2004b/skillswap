namespace Backend.DTOs;

public class StripeConnectResponse
{
    public string? Url { get; set; }

    public string? StripeAccountId { get; set; }

    public bool OnboardingComplete { get; set; }

    public bool ChargesEnabled { get; set; }

    public bool PayoutsEnabled { get; set; }
}