using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly BankingDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AdminController(BankingDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    [HttpGet("customers")]
    public async Task<ActionResult<IEnumerable<AdminCustomerSummaryResponse>>> GetCustomers(CustomerStatus? status)
    {
        var query = _dbContext.Customers
            .AsNoTracking()
            .Include(customer => customer.Accounts)
            .Include(customer => customer.KycDocuments)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(customer => customer.Status == status.Value);
        }

        var customers = await query
            .OrderByDescending(customer => customer.CreatedAtUtc)
            .Select(customer => new AdminCustomerSummaryResponse(
                customer.Id,
                customer.FirstName,
                customer.LastName,
                customer.Email,
                customer.Status,
                customer.PhoneNumber,
                customer.DateOfBirth,
                customer.Address,
                customer.NationalIdNumber,
                customer.Occupation,
                customer.EmployerName,
                customer.MonthlyIncome,
                customer.RejectionReason,
                customer.CreatedAtUtc,
                customer.ApprovedAtUtc,
                customer.Accounts.Count,
                customer.KycDocuments.Count,
                customer.Accounts.Select(account => ToAccountResponse(account)).ToList(),
                customer.KycDocuments.Select(document => ToKycDocumentResponse(document)).ToList()))
            .ToListAsync();

        return Ok(customers);
    }

    [HttpPost("customers/{id:guid}/approve")]
    public async Task<ActionResult<AdminCustomerSummaryResponse>> ApproveCustomer(Guid id)
    {
        var customer = await _dbContext.Customers
            .Include(existingCustomer => existingCustomer.Accounts)
            .Include(existingCustomer => existingCustomer.KycDocuments)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Id == id);

        if (customer is null)
        {
            return NotFound();
        }

        customer.Status = CustomerStatus.Active;
        customer.ApprovedAtUtc = DateTime.UtcNow;
        customer.ApprovedByAdminId = GetCurrentAdminId();
        customer.RejectionReason = null;

        if (customer.Accounts.Count == 0)
        {
            customer.Accounts.Add(new BankAccount
            {
                Customer = customer,
                AccountNumber = GenerateAccountNumber(),
                AccountType = "Savings",
                Currency = "GHS",
                Balance = 0,
                DailyTransferLimit = 10000m,
                DailyWithdrawalLimit = 5000m
            });
        }

        await _dbContext.SaveChangesAsync();

        return Ok(ToAdminCustomerSummary(customer));
    }

    [HttpGet("kyc-documents/{id:guid}/file")]
    public async Task<IActionResult> GetKycDocumentFile(Guid id, bool download = false)
    {
        var document = await _dbContext.KycDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(existingDocument => existingDocument.Id == id);

        if (document is null)
        {
            return NotFound();
        }

        var filePath = Path.Combine(_environment.ContentRootPath, "UploadedKycDocuments", document.StoredFileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Document file was not found on disk." });
        }

        if (download)
        {
            return PhysicalFile(filePath, document.ContentType, document.OriginalFileName);
        }

        Response.Headers.Remove("X-Frame-Options");
        Response.Headers.ContentSecurityPolicy = "frame-ancestors 'self' http://localhost:5173";
        Response.Headers.ContentDisposition = $"inline; filename=\"{document.OriginalFileName}\"";
        return PhysicalFile(filePath, document.ContentType, enableRangeProcessing: true);
    }

    [HttpPost("customers/{id:guid}/reject")]
    public async Task<ActionResult<AdminCustomerSummaryResponse>> RejectCustomer(Guid id, ReviewCustomerRequest request)
    {
        var customer = await _dbContext.Customers
            .Include(existingCustomer => existingCustomer.Accounts)
            .Include(existingCustomer => existingCustomer.KycDocuments)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Id == id);

        if (customer is null)
        {
            return NotFound();
        }

        customer.Status = CustomerStatus.Rejected;
        customer.ApprovedAtUtc = null;
        customer.ApprovedByAdminId = null;
        customer.RejectionReason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Registration did not pass review."
            : request.Reason.Trim();

        await _dbContext.SaveChangesAsync();

        return Ok(ToAdminCustomerSummary(customer));
    }

    private Guid? GetCurrentAdminId()
    {
        var adminIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(adminIdValue, out var adminId) ? adminId : null;
    }

    private static AdminCustomerSummaryResponse ToAdminCustomerSummary(Customer customer)
    {
        return new AdminCustomerSummaryResponse(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.Status,
            customer.PhoneNumber,
            customer.DateOfBirth,
            customer.Address,
            customer.NationalIdNumber,
            customer.Occupation,
            customer.EmployerName,
            customer.MonthlyIncome,
            customer.RejectionReason,
            customer.CreatedAtUtc,
            customer.ApprovedAtUtc,
            customer.Accounts.Count,
            customer.KycDocuments.Count,
            customer.Accounts.Select(ToAccountResponse).ToList(),
            customer.KycDocuments.Select(ToKycDocumentResponse).ToList());
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

    private static string GenerateAccountNumber()
    {
        return $"10{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}
