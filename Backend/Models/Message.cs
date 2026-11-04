using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;

    public ApplicationUser? Sender { get; set; }

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    public ApplicationUser? Receiver { get; set; }

    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }

    public bool IsDeletedBySender { get; set; }

    public bool IsDeletedByReceiver { get; set; }
}