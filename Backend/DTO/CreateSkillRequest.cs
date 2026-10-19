using System.ComponentModel.DataAnnotations;
namespace Backend.DTOs;

public class CreateSkillRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Category { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal? HourlyRate { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "USD";
}
