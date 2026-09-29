namespace PersonalFinance.Api.Entities;

public class Budget
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CategoryId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; }

    public Category Category { get; set; } = null!;
}