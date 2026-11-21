using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class SkillSwapRequest
{
    public int Id { get; set; }
    [Required] public string ProposerId { get; set; } = string.Empty;
    public AuthenticationUser? Proposer { get; set; }
    [Required] public string RecipientId { get; set; } = string.Empty;
    public AuthenticationUser? Recipient { get; set; }
    [Required] public int OfferedSkillId { get; set; }
    public Skill? OfferedSkill { get; set; }
    [Required] public int RequestedSkillId { get; set; }
    public Skill? RequestedSkill { get; set; }
    [Required, MaxLength(30)] public string Status { get; set; } = "Pending";
    [MaxLength(1000)] public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
    public bool ProposerConfirmedCompletion { get; set; }
    public bool RecipientConfirmedCompletion { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
