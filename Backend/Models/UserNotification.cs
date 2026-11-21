using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class UserNotification
{
    public long Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public AuthenticationUser? User { get; set; }
    [Required, MaxLength(80)] public string Type { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string Message { get; set; } = string.Empty;
    [MaxLength(100)] public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
