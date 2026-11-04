using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Booking
{
    public int Id { get; set; }

    [Required]
    public int SkillId { get; set; }

    public Skill? Skill { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public ApplicationUser? Student { get; set; }

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public ApplicationUser? Teacher { get; set; }

    [Required]
    public DateTime StartAtUtc { get; set; }

    [Required]
    public DateTime EndAtUtc { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalPrice { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}