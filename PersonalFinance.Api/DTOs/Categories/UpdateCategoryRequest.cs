namespace PersonalFinance.Api.DTOs.Categories;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;

    public string CategoryType { get; set; } = string.Empty;
}