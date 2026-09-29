using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Transaction;
using PersonalFinance.Api.Entities;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TransactionsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: transactions
    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        var userId = GetUserId();

        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        return Ok(transactions);
    }

    // GET: transactions/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        var userId = GetUserId();

        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new
            {
                message = "Transaction not found."
            });
        }

        return Ok(transaction);
    }

    // POST: transactions
    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request)
    {
        var userId = GetUserId();

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Amount must be greater than zero."
            });
        }

        if (request.TransactionType != "income" &&
            request.TransactionType != "expense")
        {
            return BadRequest(new
            {
                message = "Transaction type must be income or expense."
            });
        }

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == request.AccountId &&
                a.UserId == userId);

        if (account == null)
        {
            return NotFound(new
            {
                message = "Account not found."
            });
        }

        var category = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId));

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        // Pastikan category sesuai dengan transaction type
        if (category.CategoryType != request.TransactionType)
        {
            return BadRequest(new
            {
                message =
                    $"Category type must be '{request.TransactionType}'."
            });
        }

        // Expense tidak boleh melebihi saldo account
        if (request.TransactionType == "expense" &&
            account.Balance < request.Amount)
        {
            return BadRequest(new
            {
                message = "Insufficient account balance."
            });
        }

        await using var dbTransaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                TransactionType = request.TransactionType,
                Amount = request.Amount,
                TransactionDate = request.TransactionDate,
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);

            ApplyTransactionToBalance(
                account,
                request.TransactionType,
                request.Amount);

            await _db.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetTransaction),
                new { id = transaction.Id },
                transaction);
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    // PUT: transactions/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTransaction(
        Guid id,
        [FromBody] UpdateTransactionRequest request)
    {
        var userId = GetUserId();

        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Amount must be greater than zero."
            });
        }

        if (request.TransactionType != "income" &&
            request.TransactionType != "expense")
        {
            return BadRequest(new
            {
                message = "Transaction type must be income or expense."
            });
        }

        var existingTransaction = await _db.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId);

        if (existingTransaction == null)
        {
            return NotFound(new
            {
                message = "Transaction not found."
            });
        }

        var oldAccount = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == existingTransaction.AccountId &&
                a.UserId == userId);

        if (oldAccount == null)
        {
            return NotFound(new
            {
                message = "Original account not found."
            });
        }

        var newAccount = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == request.AccountId &&
                a.UserId == userId);

        if (newAccount == null)
        {
            return NotFound(new
            {
                message = "Account not found."
            });
        }

        var category = await _db.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                (c.UserId == null || c.UserId == userId));

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        if (category.CategoryType != request.TransactionType)
        {
            return BadRequest(new
            {
                message =
                    $"Category type must be '{request.TransactionType}'."
            });
        }

        await using var dbTransaction =
        await _db.Database.BeginTransactionAsync();

    try
    {
        // 1. Kembalikan saldo akibat transaksi lama
        ReverseTransactionFromBalance(
            oldAccount,
            existingTransaction.TransactionType,
            existingTransaction.Amount);

        // 2. Pastikan account baru cukup
        if (request.TransactionType == "expense" &&
            newAccount.Balance < request.Amount)
        {
            await dbTransaction.RollbackAsync();

            return BadRequest(new
            {
                message = "Insufficient account balance."
            });
        }

        // 3. Terapkan transaksi baru
        ApplyTransactionToBalance(
            newAccount,
            request.TransactionType,
            request.Amount);

        // 4. Update data transaksi
        existingTransaction.AccountId = request.AccountId;
        existingTransaction.CategoryId = request.CategoryId;
        existingTransaction.TransactionType = request.TransactionType;
        existingTransaction.Amount = request.Amount;
        existingTransaction.TransactionDate = request.TransactionDate;
        existingTransaction.Description =
            request.Description?.Trim();
        existingTransaction.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await dbTransaction.CommitAsync();

        return NoContent();
    }
    catch
    {
        await dbTransaction.RollbackAsync();
        throw;
    }
    }

    // DELETE: transactions/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = GetUserId();

        var existingTransaction = await _db.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId);

        if (existingTransaction == null)
        {
            return NotFound(new
            {
                message = "Transaction not found."
            });
        }

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == existingTransaction.AccountId &&
                a.UserId == userId);

        if (account == null)
        {
            return NotFound(new
            {
                message = "Account not found."
            });
        }

        await using var dbTransaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            ReverseTransactionFromBalance(
                account,
                existingTransaction.TransactionType,
                existingTransaction.Amount);

            _db.Transactions.Remove(existingTransaction);

            await _db.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return NoContent();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private static void ApplyTransactionToBalance(
        Account account,
        string transactionType,
        decimal amount)
    {
        if (transactionType == "income")
        {
            account.Balance += amount;
        }
        else
        {
            account.Balance -= amount;
        }

        account.UpdatedAt = DateTime.UtcNow;
    }

    private static void ReverseTransactionFromBalance(
        Account account,
        string transactionType,
        decimal amount)
    {
        if (transactionType == "income")
        {
            account.Balance -= amount;
        }
        else
        {
            account.Balance += amount;
        }

        account.UpdatedAt = DateTime.UtcNow;
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