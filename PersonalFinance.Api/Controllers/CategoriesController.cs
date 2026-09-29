using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Categories;
using PersonalFinance.Api.Entities;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET:categories
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var userId = GetUserId();

        var categories = await _db.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }

    // POST:categories
    [HttpPost]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Category name is required."
            });
        }

        if (request.CategoryType != "income" &&
            request.CategoryType != "expense")
        {
            return BadRequest(new
            {
                message = "Category type must be income or expense."
            });
        }

        var trimmedName = request.Name.Trim();

        // Cek duplikat: kombinasi (UserId, Name, CategoryType) harus unik
        var duplicateExists = await _db.Categories
            .AnyAsync(c =>
                c.UserId == userId &&
                c.Name.ToLower() == trimmedName.ToLower() &&
                c.CategoryType == request.CategoryType);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = $"Category '{trimmedName}' with type '{request.CategoryType}' already exists."
            });
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = trimmedName,
            CategoryType = request.CategoryType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Fallback jika unique constraint di DB ter-trigger
            // (mis. race condition antara cek dan insert)
            return Conflict(new
            {
                message = $"Category '{trimmedName}' with type '{request.CategoryType}' already exists."
            });
        }

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = category.Id },
            category
        );
    }

    // GET:categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var userId = GetUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                (c.UserId == null || c.UserId == userId));

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(category);
    }

    // PUT:categories/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request)
    {
        var userId = GetUserId();

        var existingCategory = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.UserId == userId);

        if (existingCategory == null)
        {
            return NotFound(new
            {
                message = "Category not found or cannot be modified."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Category name is required."
            });
        }

        if (request.CategoryType != "income" &&
            request.CategoryType != "expense")
        {
            return BadRequest(new
            {
                message = "Category type must be income or expense."
            });
        }

        var trimmedName = request.Name.Trim();

        // Cek duplikat: pastikan tidak ada kategori lain (selain yang sedang diupdate)
        // dengan kombinasi (UserId, Name, CategoryType) yang sama
        var duplicateExists = await _db.Categories
            .AnyAsync(c =>
                c.Id != id &&
                c.UserId == userId &&
                c.Name.ToLower() == trimmedName.ToLower() &&
                c.CategoryType == request.CategoryType);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = $"Category '{trimmedName}' with type '{request.CategoryType}' already exists."
            });
        }

        existingCategory.Name = trimmedName;
        existingCategory.CategoryType = request.CategoryType;
        existingCategory.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = $"Category '{trimmedName}' with type '{request.CategoryType}' already exists."
            });
        }

        return NoContent();
    }

    // DELETE:categories/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = GetUserId();

        var existingCategory = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.UserId == userId);

        if (existingCategory == null)
        {
            return NotFound(new
            {
                message = "Category not found or cannot be deleted."
            });
        }

        _db.Categories.Remove(existingCategory);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException();
        }

        return id;
    }
}