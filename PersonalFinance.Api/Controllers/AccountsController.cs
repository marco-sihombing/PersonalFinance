using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.DTOs.Accounts;
using PersonalFinance.Api.Entities;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AccountsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Accounts
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = GetUserId();

        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        return Ok(accounts);
    }

    // GET: /Accounts/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var userId = GetUserId();

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.UserId == userId);

        if (account == null)
        {
            return NotFound(new
            {
                message = "Account not found."
            });
        }

        return Ok(account);
    }

    // POST: /Accounts
    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountsRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Account name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.AccountType))
        {
            return BadRequest(new
            {
                message = "Account type is required."
            });
        }

        if (request.OpeningBalance < 0)
        {
            return BadRequest(new
            {
                message = "Opening balance cannot be negative."
            });
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            AccountType = request.AccountType.Trim(),
            Balance = request.OpeningBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAccountById),
            new { id = account.Id },
            account);
    }

    // PUT: /Accounts/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAccount(
        Guid id,
        [FromBody] UpdateAccountsRequest request)
    {
        var userId = GetUserId();

        var existingAccount = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.UserId == userId);

        if (existingAccount == null)
        {
            return NotFound(new
            {
                message = "Account not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Account name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.AccountType))
        {
            return BadRequest(new
            {
                message = "Account type is required."
            });
        }

        // Balance sengaja TIDAK diubah di sini.
        // Balance hanya berubah melalui Transaction.

        existingAccount.Name = request.Name.Trim();
        existingAccount.AccountType = request.AccountType.Trim();
        existingAccount.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(existingAccount);
    }

    // DELETE: /Accounts/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var userId = GetUserId();

        var existingAccount = await _db.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.UserId == userId);

        if (existingAccount == null)
        {
            return NotFound(new
            {
                message = "Account not found."
            });
        }

        // Jangan menghapus account yang sudah memiliki transaksi.
        var hasTransactions = await _db.Transactions
            .AnyAsync(t =>
                t.AccountId == id &&
                t.UserId == userId);

        if (hasTransactions)
        {
            return Conflict(new
            {
                message =
                    "This account cannot be deleted because it has transactions."
            });
        }

        _db.Accounts.Remove(existingAccount);

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