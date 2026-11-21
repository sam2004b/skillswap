using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateSkillSwapRequest
{
    [Required] public int OfferedSkillId { get; set; }
    [Required] public int RequestedSkillId { get; set; }
    [StringLength(1000)] public string? Message { get; set; }
}
