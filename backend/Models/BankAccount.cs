namespace ApiDemo.Models;

public class BankAccount
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Savings";
    public string Currency { get; set; } = "GHS";
    public decimal Balance { get; set; }
    public decimal DailyTransferLimit { get; set; } = 10000m;
    public decimal DailyWithdrawalLimit { get; set; } = 5000m;
    public bool AllowInternationalTransfers { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public List<BankTransaction> Transactions { get; set; } = [];
}
