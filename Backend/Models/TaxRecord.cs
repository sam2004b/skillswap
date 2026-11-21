
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class TaxRecord
{
    public int Id { get; set; }

    [Required]
    public int PaymentId { get; set; }

    public Payment? Payment { get; set; }

    [MaxLength(2)]
    public string CustomerCountry { get; set; } = string.Empty;

    [MaxLength(2)]
    public string TeacherCountry { get; set; } = string.Empty;

    [Column(TypeName = "numeric(18,2)")]
    public decimal TaxableAmount { get; set; }

    [Column(TypeName = "numeric(9,4)")]
    public decimal TaxRate { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TaxAmount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [MaxLength(100)]
    public string TaxJurisdiction { get; set; } = string.Empty;

    [MaxLength(30)]
    public string TaxStatus { get; set; } = "PendingTaxRules";

    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}
