namespace Backend.DTOs;

public class TaxProfileResponse
{
    public string CountryCode { get; set; } = string.Empty;

    public string? TaxIdentificationNumber { get; set; }

    public string CustomerType { get; set; } = string.Empty;

    public bool IsBusinessRegistered { get; set; }
}
