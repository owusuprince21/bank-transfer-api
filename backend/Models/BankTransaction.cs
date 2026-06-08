namespace ApiDemo.Models;

public class BankTransaction
{
    public Guid Id { get; set; }
    public Guid BankAccountId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfterTransaction { get; set; }
    public string? Description { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BankAccount? BankAccount { get; set; }
}
