namespace PersonalFinance.Api.Entities;

public class Category
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CategoryType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}