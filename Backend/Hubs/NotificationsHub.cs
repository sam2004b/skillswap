using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private const string UserGroupPrefix = "user:";

    public static string UserGroup(string userId)
    {
        return $"{UserGroupPrefix}{userId}";
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            Context.Abort();
            return;
        }

        var groupName = UserGroup(userId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName);

        await base.OnConnectedAsync();
    }
}
