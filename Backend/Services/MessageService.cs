using Backend.Data;
using Backend.DTOs.Messages;
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

    public async Task<MessageResponse> SendMessageAsync(
        string senderId,
        SendMessageRequest request)
    {
        var content = request.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Message content cannot be empty.");
        }

        if (content.Length > 5000)
        {
            throw new InvalidOperationException(
                "Message cannot exceed 5000 characters.");
        }

        if (senderId == request.ReceiverId)
        {
            throw new InvalidOperationException(
                "You cannot send a message to yourself.");
        }

        var sender = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == senderId && u.IsActive);

        if (sender == null)
        {
            throw new InvalidOperationException(
                "Sender account is not available.");
        }

        var receiver = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == request.ReceiverId && u.IsActive);

        if (receiver == null)
        {
            throw new KeyNotFoundException(
                "Recipient not found or inactive.");
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Content = content,
            SentAtUtc = DateTime.UtcNow
        };

        _context.Messages.Add(message);

        await _context.SaveChangesAsync();

        return new MessageResponse
        {
            Id = message.Id,
            SenderId = sender.Id,
            SenderName = sender.FullName,
            ReceiverId = receiver.Id,
            ReceiverName = receiver.FullName,
            Content = message.Content,
            SentAtUtc = message.SentAtUtc,
            ReadAtUtc = message.ReadAtUtc,
            IsRead = false
        };
    }

    public async Task<List<ConversationResponse>> GetConversationsAsync(
        string userId)
    {
        var messages = await _context.Messages
            .AsNoTracking()
            .Where(m =>
                (m.SenderId == userId && !m.IsDeletedBySender) ||
                (m.ReceiverId == userId && !m.IsDeletedByReceiver))
            .OrderByDescending(m => m.SentAtUtc)
            .Select(m => new
            {
                m.Id,
                m.SenderId,
                m.ReceiverId,
                m.Content,
                m.SentAtUtc,
                m.ReadAtUtc,
                SenderName = m.Sender!.FullName,
                ReceiverName = m.Receiver!.FullName
            })
            .ToListAsync();

        var conversations = messages
            .GroupBy(m =>
                m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(group =>
            {
                var latest = group.First();

                var otherUserIsSender = latest.SenderId != userId;

                return new ConversationResponse
                {
                    OtherUserId = group.Key,
                    OtherUserName = otherUserIsSender
                        ? latest.SenderName
                        : latest.ReceiverName,
                    LastMessage = latest.Content,
                    LastMessageAtUtc = latest.SentAtUtc,
                    UnreadCount = group.Count(m =>
                        m.ReceiverId == userId &&
                        m.ReadAtUtc == null)
                };
            })
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToList();

        return conversations;
    }

    public async Task<List<MessageResponse>> GetConversationAsync(
        string userId,
        string otherUserId)
    {
        if (userId == otherUserId)
        {
            throw new InvalidOperationException(
                "You cannot open a conversation with yourself.");
        }

        var otherUserExists = await _context.Users
            .AnyAsync(u => u.Id == otherUserId && u.IsActive);

        if (!otherUserExists)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return await _context.Messages
            .AsNoTracking()
            .Where(m =>
                ((m.SenderId == userId &&
                  m.ReceiverId == otherUserId) &&
                  !m.IsDeletedBySender) ||
                ((m.SenderId == otherUserId &&
                  m.ReceiverId == userId) &&
                  !m.IsDeletedByReceiver))
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender!.FullName,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver!.FullName,
                Content = m.Content,
                SentAtUtc = m.SentAtUtc,
                ReadAtUtc = m.ReadAtUtc,
                IsRead = m.ReadAtUtc != null
            })
            .ToListAsync();
    }

    public async Task<int> MarkConversationAsReadAsync(
        string userId,
        string otherUserId)
    {
        var messages = await _context.Messages
            .Where(m =>
                m.SenderId == otherUserId &&
                m.ReceiverId == userId &&
                m.ReadAtUtc == null &&
                !m.IsDeletedByReceiver)
            .ToListAsync();

        var readTime = DateTime.UtcNow;

        foreach (var message in messages)
        {
            message.ReadAtUtc = readTime;
        }

        await _context.SaveChangesAsync();

        return messages.Count;
    }

    public async Task DeleteMessageAsync(
        int messageId,
        string userId)
    {
        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
        {
            throw new KeyNotFoundException("Message not found.");
        }

        if (message.SenderId == userId)
        {
            if (message.IsDeletedBySender)
            {
                throw new InvalidOperationException(
                    "Message is already deleted from your view.");
            }

            message.IsDeletedBySender = true;
        }
        else if (message.ReceiverId == userId)
        {
            if (message.IsDeletedByReceiver)
            {
                throw new InvalidOperationException(
                    "Message is already deleted from your view.");
            }

            message.IsDeletedByReceiver = true;
        }
        else
        {
            throw new UnauthorizedAccessException(
                "You cannot delete this message.");
        }

        await _context.SaveChangesAsync();
    }
}