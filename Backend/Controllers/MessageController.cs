
using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpPost]
    public async Task<IActionResult> Send(SendMessageRequest request)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        try
        {
            var message = await _messageService.SendAsync(
                request, userId);

            return StatusCode(201, message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        return Ok(
            await _messageService.GetConversationsAsync(userId));
    }

    [HttpGet("conversations/{otherUserId}")]
    public async Task<IActionResult> GetConversation(
        string otherUserId)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        var messages = await _messageService.GetConversationAsync(
            userId, otherUserId);

        return messages == null ? NotFound() : Ok(messages);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        var success = await _messageService.MarkAsReadAsync(
            id, userId);

        return success
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId is not string userId)
            return Unauthorized();

        var success = await _messageService.DeleteAsync(id, userId);

        return success
            ? NoContent()
            : NotFound();
    }
}
