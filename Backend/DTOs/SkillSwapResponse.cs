namespace Backend.DTOs;

public class SkillSwapResponse
{
    public int Id { get; set; }
    public string ProposerId { get; set; } = string.Empty;
    public string ProposerName { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public int OfferedSkillId { get; set; }
    public string OfferedSkillTitle { get; set; } = string.Empty;
    public int RequestedSkillId { get; set; }
    public string RequestedSkillTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
