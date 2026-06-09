namespace ApiDemo.Models;

public class SpendingControl
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal MonthlySpendLimit { get; set; }
    public decimal SingleTransactionLimit { get; set; }
    public decimal SavingsTarget { get; set; }
    public bool BlockTransfersWhenLimitReached { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}
