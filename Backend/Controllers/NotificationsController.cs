using System.Security.Claims;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IUserNotificationService _service;
    public NotificationsController(IUserNotificationService service) => _service = service;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found.");

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct) => Ok(await _service.GetMineAsync(UserId, ct));

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct) => await _service.MarkReadAsync(UserId, id, ct) ? NoContent() : NotFound();
}
