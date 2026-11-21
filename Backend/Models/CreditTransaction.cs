using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class CreditTransaction
{
    public long Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;

    public CreditWallet? Wallet { get; set; }

    [Required, MaxLength(30)] 
    public string Type { get; set; } = string.Empty;

    [Column(TypeName = "numeric(18,2)")]
     public decimal Amount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
     public decimal BalanceAfter { get; set; }

    public int? BookingId { get; set; }
    
    [MaxLength(300)]
     public string Description { get; set; } = string.Empty;

    [Required, MaxLength(100)]
     public string Reference { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
