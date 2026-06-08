using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            .FirstOrDefaultAsync(existingTransaction => existingTransaction.Id == id);

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

        if (customerId.HasValue)
        {
            query = query.Where(transaction => transaction.BankAccount != null && transaction.BankAccount.CustomerId == customerId);
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
            transaction.TransactionType,
            transaction.Amount,
            transaction.BalanceAfterTransaction,
            transaction.Description,
            transaction.ReferenceNumber,
            transaction.CreatedAtUtc);
    }
}
