using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Skills;

public class UpdateSkillRequest
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Beginner|Intermediate|Advanced)$")]
    public string Level { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal HourlyRate { get; set; }

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; set; } = "USD";
}