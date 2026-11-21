
using Backend.Data;
using Backend.DTOs;
using Backend.Hubs;
using Backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class UserNotificationService : IUserNotificationService
{
    private const int MaximumNotifications = 100;
    private const string NotificationEvent = "NotificationReceived";

    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationsHub> _hub;

    public UserNotificationService(
        ApplicationDbContext db,
        IHubContext<NotificationsHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task NotifyAsync(
        string userId,
        string type,
        string title,
        string message,
        string? entityType = null,
        string? entityId = null,
        CancellationToken ct = default)
    {
        var notification = new UserNotification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId
        };

        _db.UserNotifications.Add(notification);

        await _db.SaveChangesAsync(ct);

        var response = MapToResponse(notification);

        // Deliver the notification to the user's connected clients.
        await _hub.Clients
            .Group(NotificationsHub.UserGroup(userId))
            .SendAsync(NotificationEvent, response, ct);
    }

    public async Task<List<UserNotificationResponse>> GetMineAsync(
        string userId,
        CancellationToken ct = default)
    {
        return await _db.UserNotifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(MaximumNotifications)
            .Select(notification => new UserNotificationResponse
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                RelatedEntityType = notification.RelatedEntityType,
                RelatedEntityId = notification.RelatedEntityId,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<bool> MarkReadAsync(
        string userId,
        long notificationId,
        CancellationToken ct = default)
    {
        var notification = await _db.UserNotifications
            .FirstOrDefaultAsync(
                item => item.Id == notificationId &&
                        item.UserId == userId,
                ct);

        if (notification == null)
        {
            return false;
        }

        if (notification.IsRead)
        {
            return true;
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return true;
    }

    private static UserNotificationResponse MapToResponse(
        UserNotification notification)
    {
        return new UserNotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityId = notification.RelatedEntityId,
            IsRead = notification.IsRead,
            CreatedAtUtc = notification.CreatedAtUtc
        };
    }
}
