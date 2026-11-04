using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Skill
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Level { get; set; } = string.Empty;

    [Column(TypeName = "numeric(18,2)")]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal HourlyRate { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public string UserId { get; set; } = string.Empty;

    public AuthenticationUser? User { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}