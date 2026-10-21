using System.ComponentModel.DataAnnotations;
namespace Backend.Models;

public class Availability
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    public DateTime StartAtUtc { get; set; }

    [Required]
    public DateTime EndAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
