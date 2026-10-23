
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateReviewRequest
{
    [Range(1, int.MaxValue)]
    public int BookingId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
