
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class TaxProfileRequest
{
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    public string? TaxIdentificationNumber { get; set; }

    [Required]
    public string CustomerType { get; set; } = "Individual";

    public bool IsBusinessRegistered { get; set; }
}
