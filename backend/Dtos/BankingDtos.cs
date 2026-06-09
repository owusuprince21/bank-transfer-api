using System.ComponentModel.DataAnnotations;
using ApiDemo.Models;

namespace ApiDemo.Dtos;

public record OnboardCustomerRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [MaxLength(30)] string? PhoneNumber,
    DateOnly DateOfBirth,
    [MaxLength(500)] string? Address,
    [Range(0, 9999999999999999.99)] decimal OpeningDeposit,
    [MaxLength(40)] string? AccountType);

public record RegisterCustomerRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [MaxLength(30)] string? PhoneNumber,
    DateOnly DateOfBirth,
    [MaxLength(500)] string? Address,
    [Required, MaxLength(80)] string NationalIdNumber,
    [MaxLength(120)] string? Occupation,
    [MaxLength(160)] string? EmployerName,
    [Range(0, 9999999999999999.99)] decimal? MonthlyIncome,
    [MaxLength(40)] string? RequestedAccountType);

public record CreateAccountRequest(
    [Required] Guid CustomerId,
    [MaxLength(40)] string? AccountType,
    [Range(0, 9999999999999999.99)] decimal OpeningDeposit,
    [StringLength(3, MinimumLength = 3)] string? Currency,
    [Range(0, 9999999999999999.99)] decimal? DailyTransferLimit,
    [Range(0, 9999999999999999.99)] decimal? DailyWithdrawalLimit,
    bool AllowInternationalTransfers);

public record MoneyMovementRequest(
    [Range(0.01, 9999999999999999.99)] decimal Amount,
    [MaxLength(500)] string? Description);

public record TransferRequest(
    [Required] Guid DestinationAccountId,
    [Range(0.01, 9999999999999999.99)] decimal Amount,
    [MaxLength(500)] string? Description);

public record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string Password);

public record LoginResponse(
    string Role,
    CustomerResponse? Customer,
    AdminResponse? Admin);

public record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    string? Address,
    CustomerStatus Status,
    string? NationalIdNumber,
    string? Occupation,
    string? EmployerName,
    decimal? MonthlyIncome,
    DateTime? ApprovedAtUtc,
    string? RejectionReason,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<AccountResponse> Accounts,
    IReadOnlyCollection<KycDocumentResponse> KycDocuments,
    SpendingControlResponse? SpendingControl);

public record AccountResponse(
    Guid Id,
    Guid CustomerId,
    string AccountNumber,
    string AccountType,
    string Currency,
    decimal Balance,
    decimal DailyTransferLimit,
    decimal DailyWithdrawalLimit,
    bool AllowInternationalTransfers,
    bool IsActive,
    DateTime CreatedAtUtc);

public record TransactionResponse(
    Guid Id,
    Guid BankAccountId,
    string? AccountNumber,
    string? AccountType,
    string? Currency,
    TransactionType TransactionType,
    decimal Amount,
    decimal BalanceAfterTransaction,
    string? Description,
    string ReferenceNumber,
    string? CounterpartyName,
    string? CounterpartyAccountNumber,
    string? CounterpartyAccountType,
    string? CounterpartyEmail,
    DateTime CreatedAtUtc);

public record TransferResponse(
    TransactionResponse DebitTransaction,
    TransactionResponse CreditTransaction);

public record AccountLookupResponse(
    Guid Id,
    Guid CustomerId,
    string AccountNumber,
    string AccountType,
    string CustomerName,
    string CustomerEmail);

public record RecipientCustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<AccountResponse> Accounts);

public record AdminResponse(
    Guid Id,
    string FullName,
    string Email);

public record AdminCustomerSummaryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    CustomerStatus Status,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    string? Address,
    string? NationalIdNumber,
    string? Occupation,
    string? EmployerName,
    decimal? MonthlyIncome,
    string? RejectionReason,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    int AccountCount,
    int KycDocumentCount,
    IReadOnlyCollection<AccountResponse> Accounts,
    IReadOnlyCollection<KycDocumentResponse> KycDocuments);

public record ReviewCustomerRequest(
    [MaxLength(500)] string? Reason);

public record KycDocumentResponse(
    Guid Id,
    Guid CustomerId,
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc);

public record SpendingControlRequest(
    [Range(0, 9999999999999999.99)] decimal MonthlySpendLimit,
    [Range(0, 9999999999999999.99)] decimal SingleTransactionLimit,
    [Range(0, 9999999999999999.99)] decimal SavingsTarget,
    bool BlockTransfersWhenLimitReached);

public record SpendingControlResponse(
    Guid Id,
    Guid CustomerId,
    decimal MonthlySpendLimit,
    decimal SingleTransactionLimit,
    decimal SavingsTarget,
    bool BlockTransfersWhenLimitReached,
    DateTime UpdatedAtUtc);

public record CustomerNotificationResponse(
    Guid Id,
    Guid CustomerId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc);
