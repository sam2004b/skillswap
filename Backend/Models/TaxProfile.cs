
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class TaxProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public AuthenticationUser? User { get; set; }

    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    public string? TaxIdentificationNumber { get; set; }

    [Required]
    [MaxLength(30)]
    public string CustomerType { get; set; } = "Individual";

    public bool IsBusinessRegistered { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
