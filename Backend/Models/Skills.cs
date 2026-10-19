using System.ComponentModel.DataAnnotations;
namespace Backend.Models;

public class Skill
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Category { get; set; } = string.Empty;

    // The user offering to teach this skill
    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public ApplicationUser? Teacher { get; set; }

    // Optional hourly price. Null means the listing is not priced in money.
    [Range(0, 100000)]
    public decimal? HourlyRate { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
