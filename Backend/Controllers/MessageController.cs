using Backend.DTOs.Messages;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(
                "User ID not found in token.");
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request)
    {
        try
        {
            var result = await _messageService.SendMessageAsync(
                GetUserId(), request);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var result = await _messageService.GetConversationsAsync(
            GetUserId());

        return Ok(result);
    }

    [HttpGet("conversations/{userId}")]
    public async Task<IActionResult> GetConversation(string userId)
    {
        try
        {
            var result = await _messageService.GetConversationAsync(
                GetUserId(), userId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("conversations/{userId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(
        string userId)
    {
        try
        {
            var count = await _messageService.MarkConversationAsReadAsync(
                GetUserId(), userId);

            return Ok(new
            {
                message = "Messages marked as read.",
                messagesMarkedAsRead = count
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        try
        {
            await _messageService.DeleteMessageAsync(
                id, GetUserId());

            return Ok(new
            {
                message = "Message deleted from your view."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}