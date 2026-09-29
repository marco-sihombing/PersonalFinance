namespace PersonalFinance.Api.DTOs.Categories;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;

    public string CategoryType { get; set; } = string.Empty;
}