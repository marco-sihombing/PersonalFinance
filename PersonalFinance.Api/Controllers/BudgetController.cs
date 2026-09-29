using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Budget;
using PersonalFinance.Api.Entities;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BudgetsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /Budgets
    [HttpGet]
    public async Task<IActionResult> GetBudgets()
    {
        var userId = GetUserId();

        var budgets = await _db.Budgets
            .Include(b => b.Category)
            .Where(b =>
                b.UserId == userId &&
                (b.Category.UserId == null ||
                 b.Category.UserId == userId))
            .OrderBy(b => b.PeriodStart)
            .ToListAsync();

        return Ok(budgets);
    }

    // GET /Budgets/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBudgetById(Guid id)
    {
        var userId = GetUserId();

        var budget = await _db.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId &&
                (b.Category.UserId == null ||
                 b.Category.UserId == userId));

        if (budget == null)
            return NotFound(new
            {
                message = "Budget not found."
            });

        return Ok(budget);
    }

    // POST /Budgets
    [HttpPost]
    public async Task<IActionResult> CreateBudget(
        [FromBody] CreateBudgetRequest request)
    {
        var userId = GetUserId();

        // VALIDATION

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Amount must be greater than zero."
            });
        }

        if (request.PeriodStart >= request.PeriodEnd)
        {
            return BadRequest(new
            {
                message = "Period start must be before period end."
            });
        }

        // CHECK CATEGORY

        var category = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        // Global category atau milik user sendiri
        if (category.UserId != null &&
            category.UserId != userId)
        {
            return Forbid();
        }

        // Budget hanya boleh menggunakan
        // category bertipe expense
        if (category.CategoryType != "expense")
        {
            return BadRequest(new
            {
                message = "Budget can only use expense categories."
            });
        }

        // CREATE

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            CreatedAt = DateTime.UtcNow
        };

        _db.Budgets.Add(budget);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBudgetById),
            new { id = budget.Id },
            budget);
    }

    // PUT /Budgets/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBudget(
        Guid id,
        [FromBody] UpdateBudgetRequest request)
    {
        var userId = GetUserId();

        // FIND BUDGET

        var existingBudget = await _db.Budgets
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId);

        if (existingBudget == null)
        {
            return NotFound(new
            {
                message = "Budget not found."
            });
        }

        // VALIDATION

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Amount must be greater than zero."
            });
        }

        if (request.PeriodStart >= request.PeriodEnd)
        {
            return BadRequest(new
            {
                message = "Period start must be before period end."
            });
        }

        // CHECK NEW CATEGORY

        var category = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        if (category.UserId != null &&
            category.UserId != userId)
        {
            return Forbid();
        }

        if (category.CategoryType != "expense")
        {
            return BadRequest(new
            {
                message = "Budget can only use expense categories."
            });
        }

        // UPDATE

        existingBudget.CategoryId = request.CategoryId;
        existingBudget.Amount = request.Amount;
        existingBudget.PeriodStart = request.PeriodStart;
        existingBudget.PeriodEnd = request.PeriodEnd;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /Budgets/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = GetUserId();

        var existingBudget = await _db.Budgets
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId);

        if (existingBudget == null)
        {
            return NotFound(new
            {
                message = "Budget not found."
            });
        }

        _db.Budgets.Remove(existingBudget);

        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
            throw new UnauthorizedAccessException();

        return id;
    }
}