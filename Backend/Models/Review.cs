
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Review
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    [Required]
    public string ReviewerId { get; set; } = string.Empty;
    public ApplicationUser? Reviewer { get; set; }

    [Required]
    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser? Teacher { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
