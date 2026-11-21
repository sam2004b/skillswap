
namespace Backend.DTOs;

public class TaxCalculationResponse
{
    public int BookingId { get; set; }

    public decimal ServiceAmount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "USD";

    public string CustomerCountry { get; set; } = string.Empty;

    public string TeacherCountry { get; set; } = string.Empty;

    public string TaxJurisdiction { get; set; } = string.Empty;

    public string TaxStatus { get; set; } = "PendingTaxRules";
}
