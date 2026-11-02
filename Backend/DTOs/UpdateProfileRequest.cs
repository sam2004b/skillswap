using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public string? Country { get; set; }

    public string? PhoneNumber { get; set; }
}