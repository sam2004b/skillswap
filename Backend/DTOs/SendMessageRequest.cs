using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Messages;

public class SendMessageRequest
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
}