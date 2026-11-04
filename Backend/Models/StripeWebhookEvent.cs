using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class StripeWebhookEvent
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string StripeEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }
}