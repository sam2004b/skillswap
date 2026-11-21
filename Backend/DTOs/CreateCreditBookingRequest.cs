using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateCreditBookingRequest
{
    [Required] public int SkillId { get; set; }
    [Required] public DateTime StartAtUtc { get; set; }
    [Required] public DateTime EndAtUtc { get; set; }

}
