namespace ApiDemo.Models;

public class Customer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; } = CustomerStatus.PendingApproval;
    public string? PhoneNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? NationalIdNumber { get; set; }
    public string? Occupation { get; set; }
    public string? EmployerName { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<BankAccount> Accounts { get; set; } = [];
    public List<KycDocument> KycDocuments { get; set; } = [];
    public List<CustomerNotification> Notifications { get; set; } = [];
    public SpendingControl? SpendingControl { get; set; }
}
