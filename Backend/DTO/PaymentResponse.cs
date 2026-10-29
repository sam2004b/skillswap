namespace Backend.DTOs;
public class PaymentResponse
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = string.Empty;
    public long PlatformFeeMinor { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
