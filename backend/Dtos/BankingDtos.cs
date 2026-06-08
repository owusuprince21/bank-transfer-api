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

public record CreateAccountRequest(
    [Required] Guid CustomerId,
    [MaxLength(40)] string? AccountType,
    [Range(0, 9999999999999999.99)] decimal OpeningDeposit);

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
    CustomerResponse Customer);

public record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    string? Address,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<AccountResponse> Accounts);

public record AccountResponse(
    Guid Id,
    Guid CustomerId,
    string AccountNumber,
    string AccountType,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAtUtc);

public record TransactionResponse(
    Guid Id,
    Guid BankAccountId,
    TransactionType TransactionType,
    decimal Amount,
    decimal BalanceAfterTransaction,
    string? Description,
    string ReferenceNumber,
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
