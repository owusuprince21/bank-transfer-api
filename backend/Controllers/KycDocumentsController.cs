using ApiDemo.Data;
using ApiDemo.Dtos;
using ApiDemo.Models;
using ApiDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace ApiDemo.Controllers;

[ApiController]
[Route("api/kyc-documents")]
[Authorize]
public class KycDocumentsController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private readonly BankingDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly CustomerNotificationService _notificationService;

    public KycDocumentsController(
        BankingDbContext dbContext,
        IWebHostEnvironment environment,
        CustomerNotificationService notificationService)
    {
        _dbContext = dbContext;
        _environment = environment;
        _notificationService = notificationService;
    }

    [HttpPost]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<KycDocumentResponse>> UploadDocument([FromForm] string documentType, [FromForm] IFormFile file)
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var customerExists = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.Id == customerId.Value);

        if (!customerExists)
        {
            return Unauthorized(new { message = "Your session is no longer linked to an active customer record. Please sign out and sign in again." });
        }

        if (string.IsNullOrWhiteSpace(documentType))
        {
            return BadRequest(new { message = "Document type is required." });
        }

        var normalizedDocumentType = documentType.Trim();

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Choose a PDF or image document to upload." });
        }

        if (file.Length > 5_000_000)
        {
            return BadRequest(new { message = $"{file.FileName} is too large. Upload documents up to 5MB each." });
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = $"{file.FileName} is not supported. Upload PDF, JPG, PNG, or WEBP documents only." });
        }

        var uploadRoot = Path.Combine(_environment.ContentRootPath, "UploadedKycDocuments");
        Directory.CreateDirectory(uploadRoot);

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{customerId}-{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(uploadRoot, storedFileName);

        await using (var stream = System.IO.File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        var existingDocument = await _dbContext.KycDocuments
            .FirstOrDefaultAsync(document => document.CustomerId == customerId.Value
                && document.DocumentType == normalizedDocumentType);

        var document = existingDocument ?? new KycDocument
        {
            CustomerId = customerId.Value,
            DocumentType = normalizedDocumentType
        };

        var previousStoredFileName = existingDocument?.StoredFileName;

        document.OriginalFileName = Path.GetFileName(file.FileName);
        document.StoredFileName = storedFileName;
        document.ContentType = file.ContentType;
        document.SizeBytes = file.Length;
        document.UploadedAtUtc = DateTime.UtcNow;

        if (existingDocument is null)
        {
            _dbContext.KycDocuments.Add(document);
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sqlException && sqlException.Number == 547)
        {
            if (System.IO.File.Exists(storedPath))
            {
                System.IO.File.Delete(storedPath);
            }

            return Unauthorized(new { message = "Your session is no longer linked to an active customer record. Please sign out and sign in again." });
        }

        if (!string.IsNullOrWhiteSpace(previousStoredFileName))
        {
            var previousStoredPath = Path.Combine(uploadRoot, previousStoredFileName);
            if (!string.Equals(previousStoredPath, storedPath, StringComparison.OrdinalIgnoreCase)
                && System.IO.File.Exists(previousStoredPath))
            {
                System.IO.File.Delete(previousStoredPath);
            }
        }

        await _notificationService.NotifyAsync(
            customerId.Value,
            existingDocument is null ? "KycDocumentUploaded" : "KycDocumentUpdated",
            existingDocument is null ? "KYC document uploaded" : "KYC document updated",
            $"{document.DocumentType} was {(existingDocument is null ? "uploaded" : "updated")} successfully.");

        return CreatedAtAction(nameof(GetMyDocuments), ToResponse(document));
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<KycDocumentResponse>>> GetMyDocuments()
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var documents = await _dbContext.KycDocuments
            .AsNoTracking()
            .Where(document => document.CustomerId == customerId.Value)
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(document => ToResponse(document))
            .ToListAsync();

        return Ok(documents);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetMyDocumentFile(Guid id, bool download = false)
    {
        var customerId = GetCurrentCustomerId();
        if (customerId is null)
        {
            return Unauthorized();
        }

        var document = await _dbContext.KycDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(existingDocument => existingDocument.Id == id
                && existingDocument.CustomerId == customerId.Value);

        if (document is null)
        {
            return NotFound(new { message = "Document was not found for your account." });
        }

        var filePath = Path.Combine(_environment.ContentRootPath, "UploadedKycDocuments", document.StoredFileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Document file was not found on disk. Please upload it again." });
        }

        if (download)
        {
            return PhysicalFile(filePath, document.ContentType, document.OriginalFileName);
        }

        Response.Headers.Remove("X-Frame-Options");
        Response.Headers.ContentDisposition = $"inline; filename=\"{document.OriginalFileName}\"";
        return PhysicalFile(filePath, document.ContentType, enableRangeProcessing: true);
    }

    private Guid? GetCurrentCustomerId()
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(customerIdValue, out var customerId) ? customerId : null;
    }

    private static KycDocumentResponse ToResponse(KycDocument document)
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
}
