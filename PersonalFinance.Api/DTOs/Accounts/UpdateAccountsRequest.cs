namespace PersonalFinance.Api.DTOs.Accounts;
public class UpdateAccountsRequest
{
    public string Name { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;
}