using Backend.DTOs.Messages;

namespace Backend.Services;

public interface IMessageService
{
    Task<MessageResponse> SendMessageAsync(
        string senderId,
        SendMessageRequest request);

    Task<List<ConversationResponse>> GetConversationsAsync(
        string userId);

    Task<List<MessageResponse>> GetConversationAsync(
        string userId,
        string otherUserId);

    Task<int> MarkConversationAsReadAsync(
        string userId,
        string otherUserId);

    Task DeleteMessageAsync(
        int messageId,
        string userId);
}