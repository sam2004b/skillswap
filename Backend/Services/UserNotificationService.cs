using Backend.Data;
using Backend.DTOs;
using Backend.Hubs;
using Backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationsHub> _hub;
    public UserNotificationService(ApplicationDbContext db, IHubContext<NotificationsHub> hub) { _db = db; _hub = hub; }

    public async Task NotifyAsync(string userId, string type, string title, string message, string? entityType = null, string? entityId = null, CancellationToken ct = default)
    {
        var item = new UserNotification { UserId=userId, Type=type, Title=title, Message=message, RelatedEntityType=entityType, RelatedEntityId=entityId };
        _db.UserNotifications.Add(item);
        await _db.SaveChangesAsync(ct);
        var response = new UserNotificationResponse { Id=item.Id, Type=item.Type, Title=item.Title, Message=item.Message, RelatedEntityType=item.RelatedEntityType, RelatedEntityId=item.RelatedEntityId, IsRead=item.IsRead, CreatedAtUtc=item.CreatedAtUtc };
        await _hub.Clients.Group(NotificationsHub.UserGroup(userId)).SendAsync("NotificationReceived", response, ct);
    }

    public Task<List<UserNotificationResponse>> GetMineAsync(string userId, CancellationToken ct = default) => _db.UserNotifications.AsNoTracking().Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAtUtc).Take(100).Select(n => new UserNotificationResponse { Id=n.Id, Type=n.Type, Title=n.Title, Message=n.Message, RelatedEntityType=n.RelatedEntityType, RelatedEntityId=n.RelatedEntityId, IsRead=n.IsRead, CreatedAtUtc=n.CreatedAtUtc }).ToListAsync(ct);

    public async Task<bool> MarkReadAsync(string userId, long notificationId, CancellationToken ct = default)
    {
        var item = await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);
        if (item == null) return false;
        if (!item.IsRead) { item.IsRead=true; item.ReadAtUtc=DateTime.UtcNow; await _db.SaveChangesAsync(ct); }
        return true;
    }
}
