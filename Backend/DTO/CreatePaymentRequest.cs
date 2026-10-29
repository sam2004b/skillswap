using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreatePaymentRequest
{
    [Range(1, int.MaxValue)]
    public int BookingId { get; set; }
}
