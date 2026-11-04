namespace Backend.DTOs.Messages;

public class MessageResponse
{
    public int Id { get; set; }

    public string SenderId { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string ReceiverId { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset SentAtUtc { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }

    public bool IsRead { get; set; }
}