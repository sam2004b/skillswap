
using Backend.DTOs;

namespace Backend.Services;

public interface IMessageService
{
    Task<MessageResponse> SendAsync(
        SendMessageRequest request, string senderId);

    Task<List<ConversationResponse>> GetConversationsAsync(
        string userId);

    Task<List<MessageResponse>?> GetConversationAsync(
        string userId, string otherUserId);

    Task<bool> MarkAsReadAsync(int messageId, string userId);

    Task<bool> DeleteAsync(int messageId, string userId);
}
