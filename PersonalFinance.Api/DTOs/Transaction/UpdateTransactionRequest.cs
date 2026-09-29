namespace PersonalFinance.Api.DTOs.Transaction;
public class UpdateTransactionRequest
{
    public Guid AccountId { get; set; }

    public Guid CategoryId { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public string? Description { get; set; }
}