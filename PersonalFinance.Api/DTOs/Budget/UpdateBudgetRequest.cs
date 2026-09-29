namespace PersonalFinance.Api.DTOs.Budget;
public class UpdateBudgetRequest
{
    public Guid CategoryId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }
}