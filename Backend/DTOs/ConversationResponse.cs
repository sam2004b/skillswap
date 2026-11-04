namespace Backend.DTOs.Messages;

public class ConversationResponse
{
    public string OtherUserId { get; set; } = string.Empty;

    public string OtherUserName { get; set; } = string.Empty;

    public string LastMessage { get; set; } = string.Empty;

    public DateTimeOffset LastMessageAtUtc { get; set; }

    public int UnreadCount { get; set; }
}