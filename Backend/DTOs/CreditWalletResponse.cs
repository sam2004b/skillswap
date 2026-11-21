namespace Backend.DTOs;

public class CreditWalletResponse
{
    public decimal Balance { get; set; }
    public decimal EscrowBalance { get; set; }
    public IReadOnlyList<CreditTransactionResponse> Transactions { get; set; } = [];
}

public class CreditTransactionResponse
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? BookingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
