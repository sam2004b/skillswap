
namespace Backend.DTOs;

public class ConversationResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAtUtc { get; set; }
    public int UnreadCount { get; set; }
}
