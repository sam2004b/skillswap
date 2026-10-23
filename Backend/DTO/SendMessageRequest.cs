
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class SendMessageRequest
{
    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}
