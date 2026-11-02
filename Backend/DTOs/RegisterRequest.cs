using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class RegisterRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string AccountType { get; set; } = "Individual";

    public string? Country { get; set; }

    public string? PhoneNumber { get; set; }
}