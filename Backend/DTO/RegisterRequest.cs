using System.ComponentModel.DataAnnotations;
namespace Backend.DTOs;
public class RegisterRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Individual|Business)$",
        ErrorMessage = "Account type must be Individual or Business.")]
    public string AccountType { get; set; } = "Individual";

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
}
