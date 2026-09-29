namespace PersonalFinance.Api.DTOs.Accounts;
public class CreateAccountsRequest
{
    public string Name { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
}