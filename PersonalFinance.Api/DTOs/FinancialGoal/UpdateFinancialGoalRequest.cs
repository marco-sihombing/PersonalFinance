namespace PersonalFinance.Api.DTOs.FinancialGoal;
public class UpdateFinancialGoalRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public DateTime? TargetDate { get; set; }
}