using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class CreditWallet
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public AuthenticationUser? User { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal Balance { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal EscrowBalance { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
