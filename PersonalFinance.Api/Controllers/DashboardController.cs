using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    // GET /Dashboard
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();

        var now = DateTime.UtcNow;

        var currentMonth = now.Month;
        var currentYear = now.Year;

        var monthStart = new DateTime(
            currentYear,
            currentMonth,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var nextMonthStart = monthStart.AddMonths(1);

        var yearStart = new DateTime(
            currentYear,
            1,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var nextYearStart = yearStart.AddYears(1);

        // TRANSACTIONS

        var transactions = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();

        // ALL-TIME FINANCIAL SUMMARY

        var totalIncome = transactions
            .Where(t => t.TransactionType == "income")
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.TransactionType == "expense")
            .Sum(t => t.Amount);

        var totalNet = totalIncome - totalExpense;

        // FINANCIAL GOALS

        var financialGoals = await _db.FinancialGoals
            .AsNoTracking()
            .Where(fg => fg.UserId == userId)
            .OrderBy(fg => fg.TargetDate ?? DateTime.MaxValue)
            .ThenBy(fg => fg.CreatedAt)
            .ToListAsync();


        // ACCOUNTS / BALANCE

        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.AccountType,
                a.Balance
            })
            .ToListAsync();

        var totalBalance = accounts.Sum(a => a.Balance);


        // FINANCIAL GOAL ALLOCATION

        var goalPool = Math.Max(totalBalance, 0);

        var financialGoalResult = financialGoals
            .Select(goal =>
            {
                var allocation = Math.Min(
                    goalPool,
                    goal.TargetAmount);

                goalPool -= allocation;

                var progress = goal.TargetAmount > 0
                    ? Math.Min(
                        (double)(allocation / goal.TargetAmount) * 100,
                        100)
                    : 0;

                return new
                {
                    goal.Id,
                    goal.Name,
                    goal.TargetAmount,
                    CurrentAmount = allocation,
                    goal.TargetDate,
                    goal.CreatedAt,
                    goal.UpdatedAt,

                    Remaining = Math.Max(
                        goal.TargetAmount - allocation,
                        0),

                    Progress = Math.Round(
                        progress,
                        2),

                    IsCompleted =
                        allocation >= goal.TargetAmount
                };
            })
            .ToList();


        // GOAL TOTAL

        var totalGoalAllocated = financialGoalResult
            .Sum(g => g.CurrentAmount);


        // AVAILABLE MONEY

        var availableMoney = Math.Max(
            totalBalance - totalGoalAllocated,
            0);

        // MONTHLY

        var monthlyTransactions = transactions
            .Where(t =>
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonthStart)
            .ToList();

        var monthlyIncome = monthlyTransactions
            .Where(t => t.TransactionType == "income")
            .Sum(t => t.Amount);

        var monthlyExpense = monthlyTransactions
            .Where(t => t.TransactionType == "expense")
            .Sum(t => t.Amount);

        var monthlyNet =
            monthlyIncome - monthlyExpense;

        // YEARLY

        var yearlyTransactions = transactions
            .Where(t =>
                t.TransactionDate >= yearStart &&
                t.TransactionDate < nextYearStart)
            .ToList();

        var yearlyIncome = yearlyTransactions
            .Where(t => t.TransactionType == "income")
            .Sum(t => t.Amount);

        var yearlyExpense = yearlyTransactions
            .Where(t => t.TransactionType == "expense")
            .Sum(t => t.Amount);

        var yearlyNet =
            yearlyIncome - yearlyExpense;

        // TRANSACTION CATEGORY SUMMARY

        var categorySummary = monthlyTransactions
            .GroupBy(t => new
            {
                t.CategoryId,
                t.TransactionType
            })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                transactionType = g.Key.TransactionType,
                totalAmount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.totalAmount)
            .ToList();


        var balanceByAccountType = accounts
            .GroupBy(a => a.AccountType)
            .Select(g => new
            {
                accountType = g.Key,
                totalBalance = g.Sum(a => a.Balance),

                accounts = g
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.Balance
                    })
                    .ToList()
            })
            .OrderByDescending(x => x.totalBalance)
            .ToList();

        // BUDGETS

        var budgets = await _db.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.PeriodStart)
            .ToListAsync();

        // CURRENT MONTH BUDGETS

        var monthlyBudgets = budgets
            .Where(b =>
                b.PeriodStart < nextMonthStart &&
                b.PeriodEnd >= monthStart)
            .Select(b => new
            {
                b.Id,
                b.Amount,
                b.PeriodStart,
                b.PeriodEnd,

                category = b.Category == null
                    ? null
                    : new
                    {
                        b.Category.Id,
                        b.Category.Name,
                        b.Category.CategoryType
                    }
            })
            .ToList();

        // CURRENT YEAR BUDGETS

        var yearlyBudgets = budgets
            .Where(b =>
                b.PeriodStart < nextYearStart &&
                b.PeriodEnd >= yearStart)
            .Select(b => new
            {
                b.Id,
                b.Amount,
                b.PeriodStart,
                b.PeriodEnd,

                category = b.Category == null
                    ? null
                    : new
                    {
                        b.Category.Id,
                        b.Category.Name,
                        b.Category.CategoryType
                    }
            })
            .ToList();

        var monthlyBudgetAmount =
            monthlyBudgets.Sum(b => b.Amount);

        var yearlyBudgetAmount =
            yearlyBudgets.Sum(b => b.Amount);

        // RESULT

        return Ok(new
        {
            period = new
            {
                month = currentMonth,
                year = currentYear,

                monthStart,
                nextMonthStart,

                yearStart,
                nextYearStart
            },

            // FINANCIAL SUMMARY

            financial = new
            {
                totalIncome,
                totalExpense,
                totalNet,

                totalBalance,
                totalGoalAllocated,
                availableMoney
            },

            // FINANCIAL GOALS

            financialGoals = financialGoalResult,

            // TRANSACTIONS

            transactions = new
            {
                month = new
                {
                    income = monthlyIncome,
                    expense = monthlyExpense,
                    net = monthlyNet
                },

                year = new
                {
                    income = yearlyIncome,
                    expense = yearlyExpense,
                    net = yearlyNet
                },

                categorySummary
            },

            // BUDGET

            budget = new
            {
                month = new
                {
                    total = monthlyBudgetAmount,
                    items = monthlyBudgets
                },

                year = new
                {
                    total = yearlyBudgetAmount,
                    items = yearlyBudgets
                }
            },

            // BALANCE

            balance = new
            {
                total = totalBalance,
                byAccountType = balanceByAccountType,
                accounts
            }
        });
    }

    // USER ID

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