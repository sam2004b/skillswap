using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Bookings;

public class RescheduleBookingRequest
{
    [Required]
    public DateTime StartAtUtc { get; set; }

    [Required]
    public DateTime EndAtUtc { get; set; }
}