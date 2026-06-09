using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using ApiDemo.Services;
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
    private readonly CustomerNotificationService _notificationService;

    public AccountsController(BankingDbContext dbContext, CustomerNotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
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
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GHS" : request.Currency.Trim().ToUpperInvariant(),
            DailyTransferLimit = request.DailyTransferLimit ?? 10000m,
            DailyWithdrawalLimit = request.DailyWithdrawalLimit ?? 5000m,
            AllowInternationalTransfers = request.AllowInternationalTransfers,
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
        await _notificationService.NotifyAsync(
            currentCustomerId.Value,
            "AccountCreated",
            "New account created",
            $"{account.AccountType} account {account.AccountNumber} is ready.");

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
        await _notificationService.NotifyAsync(
            account.CustomerId,
            "Deposit",
            "Deposit completed",
            $"{account.Currency} {request.Amount:N2} was deposited into account {account.AccountNumber}.");

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

        var spendingError = await ValidateOutgoingSpend(account.CustomerId, account.Id, request.Amount, TransactionType.Withdrawal);
        if (spendingError is not null)
        {
            return BadRequest(new { message = spendingError });
        }

        account.Balance -= request.Amount;
        var transaction = CreateTransaction(account, TransactionType.Withdrawal, request.Amount, request.Description);
        account.Transactions.Add(transaction);

        await _dbContext.SaveChangesAsync();
        await _notificationService.NotifyAsync(
            account.CustomerId,
            "Withdrawal",
            "Withdrawal completed",
            $"{account.Currency} {request.Amount:N2} was withdrawn from account {account.AccountNumber}.");

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
            .Include(existingAccount => existingAccount.Customer)
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

        var spendingError = await ValidateOutgoingSpend(sourceAccount.CustomerId, sourceAccount.Id, request.Amount, TransactionType.TransferSent);
        if (spendingError is not null)
        {
            return BadRequest(new { message = spendingError });
        }

        var referenceNumber = GenerateReferenceNumber();
        var transferDescription = string.IsNullOrWhiteSpace(request.Description)
            ? $"Transfer to {destinationAccount.Customer?.FirstName} {destinationAccount.Customer?.LastName}".Trim()
            : request.Description.Trim();
        var sourceCustomerName = $"{sourceAccount.Customer?.FirstName} {sourceAccount.Customer?.LastName}".Trim();
        var destinationCustomerName = $"{destinationAccount.Customer?.FirstName} {destinationAccount.Customer?.LastName}".Trim();

        sourceAccount.Balance -= request.Amount;
        destinationAccount.Balance += request.Amount;

        var debitTransaction = CreateTransaction(
            sourceAccount,
            TransactionType.TransferSent,
            request.Amount,
            transferDescription,
            referenceNumber,
            string.IsNullOrWhiteSpace(destinationCustomerName) ? null : destinationCustomerName,
            destinationAccount.AccountNumber,
            destinationAccount.AccountType,
            destinationAccount.Customer?.Email);

        var creditTransaction = CreateTransaction(
            destinationAccount,
            TransactionType.TransferReceived,
            request.Amount,
            $"Transfer received from account {sourceAccount.AccountNumber}",
            referenceNumber,
            string.IsNullOrWhiteSpace(sourceCustomerName) ? null : sourceCustomerName,
            sourceAccount.AccountNumber,
            sourceAccount.AccountType,
            sourceAccount.Customer?.Email);

        sourceAccount.Transactions.Add(debitTransaction);
        destinationAccount.Transactions.Add(creditTransaction);

        await _dbContext.SaveChangesAsync();
        await _notificationService.NotifyAsync(
            sourceAccount.CustomerId,
            "TransferSent",
            "Money sent",
            $"{sourceAccount.Currency} {request.Amount:N2} was sent to {destinationCustomerName}.");
        await _notificationService.NotifyAsync(
            destinationAccount.CustomerId,
            "TransferReceived",
            "Payment received",
            $"{destinationAccount.Currency} {request.Amount:N2} was received from {sourceCustomerName}.");

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
            .Include(transaction => transaction.BankAccount)
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
            account.Currency,
            account.Balance,
            account.DailyTransferLimit,
            account.DailyWithdrawalLimit,
            account.AllowInternationalTransfers,
            account.IsActive,
            account.CreatedAtUtc);
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

    private static BankTransaction CreateTransaction(
        BankAccount account,
        TransactionType transactionType,
        decimal amount,
        string? description,
        string? referenceNumber = null,
        string? counterpartyName = null,
        string? counterpartyAccountNumber = null,
        string? counterpartyAccountType = null,
        string? counterpartyEmail = null)
    {
        return new BankTransaction
        {
            BankAccount = account,
            TransactionType = transactionType,
            Amount = amount,
            BalanceAfterTransaction = account.Balance,
            Description = description,
            ReferenceNumber = referenceNumber ?? GenerateReferenceNumber(),
            CounterpartyName = counterpartyName,
            CounterpartyAccountNumber = counterpartyAccountNumber,
            CounterpartyAccountType = counterpartyAccountType,
            CounterpartyEmail = counterpartyEmail
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

    private async Task<string?> ValidateOutgoingSpend(Guid customerId, Guid accountId, decimal amount, TransactionType transactionType)
    {
        var account = await _dbContext.BankAccounts
            .AsNoTracking()
            .FirstAsync(existingAccount => existingAccount.Id == accountId);

        var dayStartUtc = DateTime.UtcNow.Date;
        var monthStartUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var control = await _dbContext.SpendingControls
            .AsNoTracking()
            .Where(existingControl => existingControl.CustomerId == customerId)
            .OrderByDescending(existingControl => existingControl.UpdatedAtUtc)
            .FirstOrDefaultAsync();

        if (control is not null)
        {
            if (control.SingleTransactionLimit > 0 && amount > control.SingleTransactionLimit)
            {
                return $"This amount exceeds your single transaction limit of {control.SingleTransactionLimit:N2}.";
            }

            if (control.BlockTransfersWhenLimitReached && control.MonthlySpendLimit > 0)
            {
                var spentThisMonth = await _dbContext.BankTransactions
                    .AsNoTracking()
                    .Include(transaction => transaction.BankAccount)
                    .Where(transaction => transaction.BankAccount != null
                        && transaction.BankAccount.CustomerId == customerId
                        && (transaction.TransactionType == TransactionType.Withdrawal
                            || transaction.TransactionType == TransactionType.TransferSent)
                        && transaction.CreatedAtUtc >= monthStartUtc)
                    .SumAsync(transaction => transaction.Amount);

                if (spentThisMonth + amount > control.MonthlySpendLimit)
                {
                    return $"This transaction would exceed your monthly spend limit of {control.MonthlySpendLimit:N2}.";
                }
            }

            return null;
        }

        if (transactionType == TransactionType.Withdrawal && account.DailyWithdrawalLimit > 0)
        {
            var withdrawnToday = await _dbContext.BankTransactions
                .AsNoTracking()
                .Where(transaction => transaction.BankAccountId == accountId
                    && transaction.TransactionType == TransactionType.Withdrawal
                    && transaction.CreatedAtUtc >= dayStartUtc)
                .SumAsync(transaction => transaction.Amount);

            if (withdrawnToday + amount > account.DailyWithdrawalLimit)
            {
                return $"This withdrawal exceeds the account daily withdrawal limit of {account.DailyWithdrawalLimit:N2}.";
            }
        }

        if (transactionType == TransactionType.TransferSent && account.DailyTransferLimit > 0)
        {
            var transferredToday = await _dbContext.BankTransactions
                .AsNoTracking()
                .Where(transaction => transaction.BankAccountId == accountId
                    && transaction.TransactionType == TransactionType.TransferSent
                    && transaction.CreatedAtUtc >= dayStartUtc)
                .SumAsync(transaction => transaction.Amount);

            if (transferredToday + amount > account.DailyTransferLimit)
            {
                return $"This transfer exceeds the account daily transfer limit of {account.DailyTransferLimit:N2}.";
            }
        }

        return null;
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
    }
}
