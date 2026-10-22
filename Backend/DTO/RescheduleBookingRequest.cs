
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class RescheduleBookingRequest
{
    [Required]
    public DateTimeOffset StartAtUtc { get; set; }

    [Required]
    public DateTimeOffset EndAtUtc { get; set; }
}
