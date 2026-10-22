
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Booking
{
    public int Id { get; set; }

    [Required]
    public string LearnerId { get; set; } = string.Empty;
    public ApplicationUser? Learner { get; set; }

    [Required]
    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser? Teacher { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }

    // Confirmed, Cancelled, or Completed
    [Required, StringLength(20)]
    public string Status { get; set; } = "Confirmed";

    // Exchange or Paid; actual payments are handled separately
    [Required, StringLength(20)]
    public string BookingType { get; set; } = "Exchange";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
