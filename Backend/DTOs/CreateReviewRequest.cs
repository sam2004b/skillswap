using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Reviews;

public class CreateReviewRequest
{
    [Required]
    public int BookingId { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
}