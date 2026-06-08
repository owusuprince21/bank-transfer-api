namespace ApiDemo.Models;

public class BankAccount
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Savings";
    public decimal Balance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public List<BankTransaction> Transactions { get; set; } = [];
}
