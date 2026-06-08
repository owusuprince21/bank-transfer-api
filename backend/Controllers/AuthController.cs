using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using ApiDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BankingDbContext _dbContext;

    public AuthController(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Include(existingCustomer => existingCustomer.Accounts)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Email == normalizedEmail);

        if (customer is null || !PasswordHasher.Verify(request.Password, customer.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new LoginResponse(ToCustomerResponse(customer)));
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
}
