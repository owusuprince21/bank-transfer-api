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
public class CustomersController : ControllerBase
{
    private readonly BankingDbContext _dbContext;

    public CustomersController(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetCustomers()
    {
        var currentCustomerId = GetCurrentCustomerId();
        if (currentCustomerId is null)
        {
            return Unauthorized();
        }

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Include(customer => customer.Accounts)
            .Where(customer => customer.Id == currentCustomerId.Value)
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .ToListAsync();

        return Ok(customers.Select(ToCustomerResponse));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(Guid id)
    {
        if (id != GetCurrentCustomerId())
        {
            return Forbid();
        }

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Include(existingCustomer => existingCustomer.Accounts)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Id == id);

        return customer is null ? NotFound() : Ok(ToCustomerResponse(customer));
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<CustomerResponse>> OnboardCustomer(OnboardCustomerRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _dbContext.Customers.AnyAsync(customer => customer.Email == normalizedEmail);
        if (emailExists)
        {
            return Conflict(new { message = "A customer with this email already exists." });
        }

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfBirth = request.DateOfBirth,
            Address = request.Address?.Trim()
        };

        var account = new BankAccount
        {
            Customer = customer,
            AccountNumber = GenerateAccountNumber(),
            AccountType = string.IsNullOrWhiteSpace(request.AccountType) ? "Savings" : request.AccountType.Trim(),
            Balance = request.OpeningDeposit
        };

        customer.Accounts.Add(account);

        if (request.OpeningDeposit > 0)
        {
            account.Transactions.Add(CreateTransaction(
                account,
                TransactionType.Deposit,
                request.OpeningDeposit,
                "Opening deposit"));
        }

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, ToCustomerResponse(customer));
    }

    [HttpGet("{id:guid}/accounts")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetCustomerAccounts(Guid id)
    {
        if (id != GetCurrentCustomerId())
        {
            return Forbid();
        }

        var customerExists = await _dbContext.Customers.AnyAsync(customer => customer.Id == id);
        if (!customerExists)
        {
            return NotFound();
        }

        var accounts = await _dbContext.BankAccounts
            .AsNoTracking()
            .Where(account => account.CustomerId == id)
            .OrderBy(account => account.AccountNumber)
            .Select(account => ToAccountResponse(account))
            .ToListAsync();

        return Ok(accounts);
    }

    private static CustomerResponse ToCustomerResponse(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.PhoneNumber,
            customer.DateOfBirth,
            customer.Address,
            customer.CreatedAtUtc,
            customer.Accounts.Select(ToAccountResponse).ToList());
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

    private static BankTransaction CreateTransaction(
        BankAccount account,
        TransactionType transactionType,
        decimal amount,
        string? description)
    {
        return new BankTransaction
        {
            BankAccount = account,
            TransactionType = transactionType,
            Amount = amount,
            BalanceAfterTransaction = account.Balance,
            Description = description,
            ReferenceNumber = GenerateReferenceNumber()
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
