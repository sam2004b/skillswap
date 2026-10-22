
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateBookingRequest
{
    [Range(1, int.MaxValue)]
    public int SkillId { get; set; }

    [Required]
    public DateTimeOffset StartAtUtc { get; set; }

    [Required]
    public DateTimeOffset EndAtUtc { get; set; }

    [Required]
    [RegularExpression("^(Exchange|Paid)$")]
    public string BookingType { get; set; } = "Exchange";
}
