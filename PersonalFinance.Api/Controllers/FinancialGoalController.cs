using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.FinancialGoal;
using PersonalFinance.Api.Entities;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class FinancialGoalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FinancialGoalsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /FinancialGoals

    [HttpGet]
    public async Task<IActionResult> GetFinancialGoals()
    {
        var userId = GetUserId();

        var goals = await GetUserGoalsAsync(userId);

        var summary = await GetUserFinancialSummaryAsync(userId);

        var syncedGoals = SyncGoalsWithFinancialSummary(
            goals,
            summary);

        var result = syncedGoals
            .Select(ToResponse)
            .ToList();

        return Ok(result);
    }


    // GET: /FinancialGoals/{id}

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFinancialGoalById(Guid id)
    {
        var userId = GetUserId();

        // Ambil SEMUA goal terlebih dahulu
        // supaya allocation berdasarkan urutan goal tetap benar.
        var goals = await GetUserGoalsAsync(userId);

        var targetGoal = goals.FirstOrDefault(g => g.Id == id);

        if (targetGoal == null)
        {
            return NotFound(new
            {
                message = "Financial goal not found."
            });
        }

        var summary = await GetUserFinancialSummaryAsync(userId);

        var syncedGoals = SyncGoalsWithFinancialSummary(
            goals,
            summary);

        var syncedTargetGoal = syncedGoals
            .First(g => g.Id == id);

        return Ok(ToResponse(syncedTargetGoal));
    }


    // GET: /FinancialGoals/available-money

    [HttpGet("available-money")]
    public async Task<IActionResult> GetAvailableMoney()
    {
        var userId = GetUserId();

        var goals = await GetUserGoalsAsync(userId);

        var summary = await GetUserFinancialSummaryAsync(userId);

        var syncedGoals = SyncGoalsWithFinancialSummary(
            goals,
            summary);

        var totalGoalAllocated = syncedGoals
            .Sum(g => g.CurrentAmount);

        var availableMoney = Math.Max(
            summary.TotalAccountBalance - totalGoalAllocated,
            0);

        return Ok(new
        {
            totalIncome = summary.TotalIncome,
            totalExpense = summary.TotalExpense,
            totalNet = summary.TotalIncome - summary.TotalExpense,

            totalAccountBalance = summary.TotalAccountBalance,

            totalGoalAllocated,
            availableMoney
        });
    }


    // POST: /FinancialGoals

    [HttpPost]
    public async Task<IActionResult> CreateFinancialGoal(
        [FromBody] CreateFinancialGoalRequest request)
    {
        var userId = GetUserId();

        var validation = ValidateGoalInput(
            request.Name,
            request.TargetAmount,
            request.TargetDate);

        if (validation != null)
        {
            return BadRequest(validation);
        }

        var goal = new FinancialGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            TargetAmount = request.TargetAmount,

            // Akan dihitung oleh allocation engine.
            CurrentAmount = 0,

            TargetDate = request.TargetDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.FinancialGoals.Add(goal);

        await _db.SaveChangesAsync();

        // Ambil SEMUA goal setelah goal baru dibuat.
        var goals = await GetUserGoalsAsync(userId);

        var summary = await GetUserFinancialSummaryAsync(userId);

        var syncedGoals = SyncGoalsWithFinancialSummary(
            goals,
            summary);

        var syncedGoal = syncedGoals
            .First(g => g.Id == goal.Id);

        return CreatedAtAction(
            nameof(GetFinancialGoalById),
            new { id = goal.Id },
            ToResponse(syncedGoal));
    }


    // PUT: /FinancialGoals/{id}

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateFinancialGoal(
        Guid id,
        [FromBody] UpdateFinancialGoalRequest request)
    {
        var userId = GetUserId();

        var goal = await _db.FinancialGoals
            .FirstOrDefaultAsync(
                fg => fg.Id == id &&
                      fg.UserId == userId);

        if (goal == null)
        {
            return NotFound(new
            {
                message = "Financial goal not found."
            });
        }

        var validation = ValidateGoalInput(
            request.Name,
            request.TargetAmount,
            request.TargetDate);

        if (validation != null)
        {
            return BadRequest(validation);
        }

        goal.Name = request.Name.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.TargetDate = request.TargetDate;
        goal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Setelah target berubah,
        // allocation seluruh goal dihitung ulang.
        var goals = await GetUserGoalsAsync(userId);

        var summary = await GetUserFinancialSummaryAsync(userId);

        var syncedGoals = SyncGoalsWithFinancialSummary(
            goals,
            summary);

        var syncedGoal = syncedGoals
            .First(g => g.Id == id);

        return Ok(ToResponse(syncedGoal));
    }


    // DELETE: /FinancialGoals/{id}

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFinancialGoal(Guid id)
    {
        var userId = GetUserId();

        var goal = await _db.FinancialGoals
            .FirstOrDefaultAsync(
                fg => fg.Id == id &&
                      fg.UserId == userId);

        if (goal == null)
        {
            return NotFound(new
            {
                message = "Financial goal not found."
            });
        }

        _db.FinancialGoals.Remove(goal);

        await _db.SaveChangesAsync();

        return NoContent();
    }


    // HELPERS

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


    private async Task<List<FinancialGoal>> GetUserGoalsAsync(
        Guid userId)
    {
        return await _db.FinancialGoals
            .AsNoTracking()
            .Where(fg => fg.UserId == userId)
            .OrderBy(fg => fg.TargetDate ?? DateTime.MaxValue)
            .ThenBy(fg => fg.CreatedAt)
            .ToListAsync();
    }


    private static object? ValidateGoalInput(
        string? name,
        decimal targetAmount,
        DateTime? targetDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new
            {
                message = "Financial goal name is required."
            };
        }

        if (targetAmount <= 0)
        {
            return new
            {
                message = "Target amount must be greater than zero."
            };
        }

        if (targetDate.HasValue &&
            targetDate.Value.Date < DateTime.UtcNow.Date)
        {
            return new
            {
                message = "Target date cannot be in the past."
            };
        }

        return null;
    }


    // FINANCIAL SUMMARY

    private async Task<UserFinancialSummary>
        GetUserFinancialSummaryAsync(Guid userId)
    {
        // Income transaksi saja.
        var totalIncome = await _db.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionType == "income")
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        // Expense transaksi saja.
        var totalExpense = await _db.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.TransactionType == "expense")
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        var totalAccountBalance = await _db.Accounts
            .Where(a => a.UserId == userId)
            .SumAsync(a => (decimal?)a.Balance) ?? 0;


        return new UserFinancialSummary
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            TotalAccountBalance = totalAccountBalance
        };
    }


    // GOAL ALLOCATION ENGINE

    private static List<FinancialGoal>
        SyncGoalsWithFinancialSummary(
            IEnumerable<FinancialGoal> goals,
            UserFinancialSummary summary)
    {
        var goalList = goals
            .OrderBy(g => g.TargetDate ?? DateTime.MaxValue)
            .ThenBy(g => g.CreatedAt)
            .ToList();


        var pool = Math.Max(
            summary.TotalAccountBalance,
            0);


        foreach (var goal in goalList)
        {
            if (pool <= 0)
            {
                goal.CurrentAmount = 0;
                continue;
            }


            // Alokasikan uang ke goal sampai target.
            var allocation = Math.Min(
                pool,
                goal.TargetAmount);


            goal.CurrentAmount = allocation;

            pool -= allocation;
        }


        return goalList;
    }


    // RESPONSE

    private static object ToResponse(
        FinancialGoal goal)
    {
        var progress = goal.TargetAmount > 0
            ? Math.Min(
                (double)(
                    goal.CurrentAmount /
                    goal.TargetAmount
                ) * 100,
                100)
            : 0;


        var isCompleted =
            goal.CurrentAmount >= goal.TargetAmount;


        return new
        {
            goal.Id,
            goal.Name,
            goal.TargetAmount,
            goal.CurrentAmount,
            goal.TargetDate,
            goal.CreatedAt,
            goal.UpdatedAt,

            Remaining = Math.Max(
                goal.TargetAmount -
                goal.CurrentAmount,
                0),

            Progress = Math.Round(
                progress,
                2),

            IsCompleted = isCompleted
        };
    }


    // SUMMARY MODEL

    private sealed class UserFinancialSummary
    {
        public decimal TotalIncome { get; init; }

        public decimal TotalExpense { get; init; }

        public decimal TotalAccountBalance { get; init; }
    }
}