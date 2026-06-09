using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly BankingDbContext _dbContext;

    public TransactionsController(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(Guid id)
    {
        var transaction = await _dbContext.BankTransactions
            .AsNoTracking()
            .Include(existingTransaction => existingTransaction.BankAccount)
            .FirstOrDefaultAsync(existingTransaction => existingTransaction.Id == id);

        if (transaction?.BankAccount?.CustomerId != GetCurrentCustomerId())
        {
            return transaction is null ? NotFound() : Forbid();
        }

        return transaction is null ? NotFound() : Ok(ToTransactionResponse(transaction));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetTransactions(
        Guid? customerId,
        Guid? accountId,
        TransactionType? transactionType,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var query = _dbContext.BankTransactions
            .AsNoTracking()
            .Include(transaction => transaction.BankAccount)
            .AsQueryable();

        var currentCustomerId = GetCurrentCustomerId();
        if (currentCustomerId is null)
        {
            return Unauthorized();
        }

        query = query.Where(transaction => transaction.BankAccount != null && transaction.BankAccount.CustomerId == currentCustomerId.Value);

        if (customerId.HasValue && customerId != currentCustomerId.Value)
        {
            return Forbid();
        }

        if (accountId.HasValue)
        {
            query = query.Where(transaction => transaction.BankAccountId == accountId);
        }

        if (transactionType.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionType == transactionType);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAtUtc >= fromUtc);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAtUtc <= toUtc);
        }

        var transactions = await query
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Select(transaction => ToTransactionResponse(transaction))
            .ToListAsync();

        return Ok(transactions);
    }

    private static TransactionResponse ToTransactionResponse(BankTransaction transaction)
    {
        return new TransactionResponse(
            transaction.Id,
            transaction.BankAccountId,
            transaction.BankAccount?.AccountNumber,
            transaction.BankAccount?.AccountType,
            transaction.BankAccount?.Currency,
            transaction.TransactionType,
            transaction.Amount,
            transaction.BalanceAfterTransaction,
            transaction.Description,
            transaction.ReferenceNumber,
            transaction.CounterpartyName,
            transaction.CounterpartyAccountNumber,
            transaction.CounterpartyAccountType,
            transaction.CounterpartyEmail,
            transaction.CreatedAtUtc);
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
    }
}
