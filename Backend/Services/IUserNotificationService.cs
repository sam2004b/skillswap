using Backend.DTOs;

namespace Backend.Services;

public interface IUserNotificationService
{
    Task NotifyAsync(string userId, string type, string title, string message, string? entityType = null, string? entityId = null, CancellationToken ct = default);
    Task<List<UserNotificationResponse>> GetMineAsync(string userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(string userId, long notificationId, CancellationToken ct = default);
}
