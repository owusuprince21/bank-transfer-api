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
public class AccountsController : ControllerBase
{
    private readonly BankingDbContext _dbContext;

    public AccountsController(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetAccount(Guid id)
    {
        var account = await _dbContext.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == id);

        if (account is not null && account.CustomerId != GetCurrentCustomerId())
        {
            return Forbid();
        }

        return account is null ? NotFound() : Ok(ToAccountResponse(account));
    }

    [HttpGet("by-number/{accountNumber}")]
    public async Task<ActionResult<AccountLookupResponse>> GetAccountByNumber(string accountNumber)
    {
        var normalizedAccountNumber = accountNumber.Trim();
        var account = await _dbContext.BankAccounts
            .AsNoTracking()
            .Include(existingAccount => existingAccount.Customer)
            .FirstOrDefaultAsync(existingAccount => existingAccount.AccountNumber == normalizedAccountNumber);

        if (account is null || account.Customer is null)
        {
            return NotFound(new { message = "Account number was not found." });
        }

        return Ok(new AccountLookupResponse(
            account.Id,
            account.CustomerId,
            account.AccountNumber,
            account.AccountType,
            $"{account.Customer.FirstName} {account.Customer.LastName}".Trim(),
            account.Customer.Email));
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> CreateAccount(CreateAccountRequest request)
    {
        var currentCustomerId = GetCurrentCustomerId();
        if (currentCustomerId is null || request.CustomerId != currentCustomerId.Value)
        {
            return Forbid();
        }

        var customerExists = await _dbContext.Customers.AnyAsync(customer => customer.Id == currentCustomerId.Value);
        if (!customerExists)
        {
            return BadRequest(new { message = "Customer does not exist." });
        }

        var account = new BankAccount
        {
            CustomerId = currentCustomerId.Value,
            AccountNumber = GenerateAccountNumber(),
            AccountType = string.IsNullOrWhiteSpace(request.AccountType) ? "Savings" : request.AccountType.Trim(),
            Balance = request.OpeningDeposit
        };

        if (request.OpeningDeposit > 0)
        {
            account.Transactions.Add(CreateTransaction(
                account,
                TransactionType.Deposit,
                request.OpeningDeposit,
                "Opening deposit"));
        }

        _dbContext.BankAccounts.Add(account);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, ToAccountResponse(account));
    }

    [HttpPost("{id:guid}/deposits")]
    public async Task<ActionResult<TransactionResponse>> Deposit(Guid id, MoneyMovementRequest request)
    {
        var account = await _dbContext.BankAccounts
            .Include(existingAccount => existingAccount.Transactions)
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == id);

        if (account is null)
        {
            return NotFound();
        }

        if (account.CustomerId != GetCurrentCustomerId())
        {
            return Forbid();
        }

        if (!account.IsActive)
        {
            return BadRequest(new { message = "Account is inactive." });
        }

        account.Balance += request.Amount;
        var transaction = CreateTransaction(account, TransactionType.Deposit, request.Amount, request.Description);
        account.Transactions.Add(transaction);

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(TransactionsController.GetTransaction), "Transactions", new { id = transaction.Id }, ToTransactionResponse(transaction));
    }

    [HttpPost("{id:guid}/withdrawals")]
    public async Task<ActionResult<TransactionResponse>> Withdraw(Guid id, MoneyMovementRequest request)
    {
        var account = await _dbContext.BankAccounts
            .Include(existingAccount => existingAccount.Transactions)
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == id);

        if (account is null)
        {
            return NotFound();
        }

        if (account.CustomerId != GetCurrentCustomerId())
        {
            return Forbid();
        }

        if (!account.IsActive)
        {
            return BadRequest(new { message = "Account is inactive." });
        }

        if (account.Balance < request.Amount)
        {
            return BadRequest(new { message = "Insufficient funds." });
        }

        account.Balance -= request.Amount;
        var transaction = CreateTransaction(account, TransactionType.Withdrawal, request.Amount, request.Description);
        account.Transactions.Add(transaction);

        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(TransactionsController.GetTransaction), "Transactions", new { id = transaction.Id }, ToTransactionResponse(transaction));
    }

    [HttpPost("{id:guid}/transfers")]
    public async Task<ActionResult<TransferResponse>> Transfer(Guid id, TransferRequest request)
    {
        if (id == request.DestinationAccountId)
        {
            return BadRequest(new { message = "Source and destination accounts must be different." });
        }

        var sourceAccount = await _dbContext.BankAccounts
            .Include(existingAccount => existingAccount.Transactions)
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == id);

        if (sourceAccount is null)
        {
            return NotFound(new { message = "Source account was not found." });
        }

        if (sourceAccount.CustomerId != GetCurrentCustomerId())
        {
            return Forbid();
        }

        var destinationAccount = await _dbContext.BankAccounts
            .Include(existingAccount => existingAccount.Customer)
            .Include(existingAccount => existingAccount.Transactions)
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == request.DestinationAccountId);

        if (destinationAccount is null)
        {
            return BadRequest(new { message = "Destination account was not found." });
        }

        if (!sourceAccount.IsActive || !destinationAccount.IsActive)
        {
            return BadRequest(new { message = "Both accounts must be active." });
        }

        if (sourceAccount.Balance < request.Amount)
        {
            return BadRequest(new { message = "Insufficient funds." });
        }

        var referenceNumber = GenerateReferenceNumber();
        var transferDescription = string.IsNullOrWhiteSpace(request.Description)
            ? $"Transfer to {destinationAccount.Customer?.FirstName} {destinationAccount.Customer?.LastName}".Trim()
            : request.Description.Trim();

        sourceAccount.Balance -= request.Amount;
        destinationAccount.Balance += request.Amount;

        var debitTransaction = CreateTransaction(
            sourceAccount,
            TransactionType.TransferSent,
            request.Amount,
            transferDescription,
            referenceNumber);

        var creditTransaction = CreateTransaction(
            destinationAccount,
            TransactionType.TransferReceived,
            request.Amount,
            $"Transfer received from account {sourceAccount.AccountNumber}",
            referenceNumber);

        sourceAccount.Transactions.Add(debitTransaction);
        destinationAccount.Transactions.Add(creditTransaction);

        await _dbContext.SaveChangesAsync();

        return Ok(new TransferResponse(
            ToTransactionResponse(debitTransaction),
            ToTransactionResponse(creditTransaction)));
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAccountTransactions(Guid id)
    {
        var account = await _dbContext.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(existingAccount => existingAccount.Id == id);

        if (account is null)
        {
            return NotFound();
        }

        if (account.CustomerId != GetCurrentCustomerId())
        {
            return Forbid();
        }

        var transactions = await _dbContext.BankTransactions
            .AsNoTracking()
            .Where(transaction => transaction.BankAccountId == id)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .Select(transaction => ToTransactionResponse(transaction))
            .ToListAsync();

        return Ok(transactions);
    }

    private static AccountResponse ToAccountResponse(BankAccount account)
    {
        return new AccountResponse(
            account.Id,
            account.CustomerId,
            account.AccountNumber,
            account.AccountType,
            account.Balance,
            account.IsActive,
            account.CreatedAtUtc);
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

    private static BankTransaction CreateTransaction(
        BankAccount account,
        TransactionType transactionType,
        decimal amount,
        string? description,
        string? referenceNumber = null)
    {
        return new BankTransaction
        {
            BankAccount = account,
            TransactionType = transactionType,
            Amount = amount,
            BalanceAfterTransaction = account.Balance,
            Description = description,
            ReferenceNumber = referenceNumber ?? GenerateReferenceNumber()
        };
    }

    private static string GenerateAccountNumber()
    {
        return $"10{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    private static string GenerateReferenceNumber()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100000, 999999)}";
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
    }
}
