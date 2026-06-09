using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using ApiDemo.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            .Include(existingCustomer => existingCustomer.KycDocuments)
            .Include(existingCustomer => existingCustomer.SpendingControl)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Email == normalizedEmail);

        if (customer is null || !PasswordHasher.Verify(request.Password, customer.PasswordHash))
        {
            var admin = await _dbContext.SystemAdmins
                .AsNoTracking()
                .FirstOrDefaultAsync(existingAdmin => existingAdmin.Email == normalizedEmail);

            if (admin is null || !PasswordHasher.Verify(request.Password, admin.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var adminClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new(ClaimTypes.Email, admin.Email),
                new(ClaimTypes.Name, admin.FullName),
                new(ClaimTypes.Role, "Admin")
            };

            var adminIdentity = new ClaimsIdentity(adminClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(adminIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            return Ok(new LoginResponse("Admin", null, new AdminResponse(admin.Id, admin.FullName, admin.Email)));
        }

        if (customer.Status != CustomerStatus.Active)
        {
            var message = customer.Status switch
            {
                CustomerStatus.PendingApproval => "Your registration is waiting for admin approval.",
                CustomerStatus.Rejected => "Your registration was rejected. Contact support for more information.",
                CustomerStatus.Suspended => "Your account is suspended. Contact support for more information.",
                _ => "Your account is not active."
            };

            return Unauthorized(new { message });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new(ClaimTypes.Email, customer.Email),
            new(ClaimTypes.Name, $"{customer.FirstName} {customer.LastName}".Trim()),
            new(ClaimTypes.Role, "Customer")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return Ok(new LoginResponse("Customer", ToCustomerResponse(customer), null));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<LoginResponse>> Me()
    {
        if (User.IsInRole("Admin"))
        {
            var adminId = GetCurrentCustomerId();
            if (adminId is null)
            {
                return Unauthorized();
            }

            var admin = await _dbContext.SystemAdmins
                .AsNoTracking()
                .FirstOrDefaultAsync(existingAdmin => existingAdmin.Id == adminId.Value);

            return admin is null
                ? Unauthorized()
                : Ok(new LoginResponse("Admin", null, new AdminResponse(admin.Id, admin.FullName, admin.Email)));
        }

        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Include(existingCustomer => existingCustomer.Accounts)
            .Include(existingCustomer => existingCustomer.KycDocuments)
            .Include(existingCustomer => existingCustomer.SpendingControl)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Id == customerId.Value);

        return customer is null ? Unauthorized() : Ok(new LoginResponse("Customer", ToCustomerResponse(customer), null));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
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
            customer.Status,
            customer.NationalIdNumber,
            customer.Occupation,
            customer.EmployerName,
            customer.MonthlyIncome,
            customer.ApprovedAtUtc,
            customer.RejectionReason,
            customer.CreatedAtUtc,
            customer.Accounts.Select(ToAccountResponse).ToList(),
            customer.KycDocuments.Select(ToKycDocumentResponse).ToList(),
            customer.SpendingControl is null ? null : ToSpendingControlResponse(customer.SpendingControl));
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

    private static KycDocumentResponse ToKycDocumentResponse(KycDocument document)
    {
        return new KycDocumentResponse(
            document.Id,
            document.CustomerId,
            document.DocumentType,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            document.UploadedAtUtc);
    }

    private static SpendingControlResponse ToSpendingControlResponse(SpendingControl control)
    {
        return new SpendingControlResponse(
            control.Id,
            control.CustomerId,
            control.MonthlySpendLimit,
            control.SingleTransactionLimit,
            control.SavingsTarget,
            control.BlockTransfersWhenLimitReached,
            control.UpdatedAtUtc);
    }
}
