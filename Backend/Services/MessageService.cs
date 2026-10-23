
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;

    public MessageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MessageResponse> SendAsync(
        SendMessageRequest request, string senderId)
    {
        var content = request.Content.Trim();
        var receiverId = request.ReceiverId.Trim();

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content is required.");

        if (receiverId == senderId)
            throw new ArgumentException(
                "You cannot send a message to yourself.");

        var receiverExists = await _context.Users
            .AnyAsync(u => u.Id == receiverId);

        if (!receiverExists)
            throw new ArgumentException("Receiver not found.");

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAtUtc = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return await BuildResponseAsync(message.Id);
    }

    public async Task<List<ConversationResponse>> GetConversationsAsync(
        string userId)
    {
        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m =>
                (m.SenderId == userId &&
                 m.DeletedBySenderAtUtc == null) ||
                m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAtUtc)
            .Select(m => new
            {
                m.SenderId,
                m.ReceiverId,
                m.Content,
                m.SentAtUtc,
                m.ReadAtUtc
            })
            .ToListAsync();

        var conversations = messages
            .GroupBy(m =>
                m.SenderId == userId
                    ? m.ReceiverId
                    : m.SenderId)
            .Select(group =>
            {
                var latest = group.First();

                return new ConversationResponse
                {
                    UserId = group.Key,
                    LastMessage = latest.Content,
                    LastMessageAtUtc = latest.SentAtUtc,
                    UnreadCount = group.Count(m =>
                        m.ReceiverId == userId &&
                        m.ReadAtUtc == null)
                };
            })
            .ToList();

        var participantIds = conversations
            .Select(c => c.UserId)
            .ToList();

        var names = await _context.Users
            .Where(u => participantIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        foreach (var conversation in conversations)
        {
            conversation.UserName =
                names.GetValueOrDefault(conversation.UserId, "");
        }

        return conversations
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToList();
    }

    public async Task<List<MessageResponse>?> GetConversationAsync(
        string userId, string otherUserId)
    {
        var otherUserExists = await _context.Users
            .AnyAsync(u => u.Id == otherUserId);

        if (!otherUserExists)
            return null;

        return await _context.Messages
            .AsNoTracking()
            .Where(m =>
                ((m.SenderId == userId &&
                  m.ReceiverId == otherUserId) ||
                 (m.SenderId == otherUserId &&
                  m.ReceiverId == userId)) &&
                (m.SenderId != userId ||
                 m.DeletedBySenderAtUtc == null))
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender != null
                    ? m.Sender.FullName : "",
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver != null
                    ? m.Receiver.FullName : "",
                Content = m.Content,
                SentAtUtc = m.SentAtUtc,
                ReadAtUtc = m.ReadAtUtc
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(
        int messageId, string userId)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m =>
                m.Id == messageId &&
                m.ReceiverId == userId);

        if (message == null)
            return false;

        if (message.ReadAtUtc == null)
        {
            message.ReadAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> DeleteAsync(
        int messageId, string userId)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m =>
                m.Id == messageId &&
                m.SenderId == userId);

        if (message == null)
            return false;

        if (message.DeletedBySenderAtUtc == null)
        {
            message.DeletedBySenderAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    private async Task<MessageResponse> BuildResponseAsync(int id)
    {
        return await _context.Messages
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender != null
                    ? m.Sender.FullName : "",
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver != null
                    ? m.Receiver.FullName : "",
                Content = m.Content,
                SentAtUtc = m.SentAtUtc,
                ReadAtUtc = m.ReadAtUtc
            })
            .FirstAsync();
    }
}
