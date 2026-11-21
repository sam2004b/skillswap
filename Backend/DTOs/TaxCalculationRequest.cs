using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;
public class TaxCalculationRequest
{
    [Range(1, int.MaxValue)]
    public int BookingId { get; set; }
}
