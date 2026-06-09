using ApiDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiDemo.Data;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<SystemAdmin> SystemAdmins => Set<SystemAdmin>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<SpendingControl> SpendingControls => Set<SpendingControl>();
    public DbSet<CustomerNotification> CustomerNotifications => Set<CustomerNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(customer => customer.Email).IsUnique();
            entity.Property(customer => customer.Email).HasMaxLength(256);
            entity.Property(customer => customer.PasswordHash).HasMaxLength(500);
            entity.Property(customer => customer.Status)
                .HasConversion<string>()
                .HasMaxLength(30);
            entity.Property(customer => customer.FirstName).HasMaxLength(100);
            entity.Property(customer => customer.LastName).HasMaxLength(100);
            entity.Property(customer => customer.PhoneNumber).HasMaxLength(30);
            entity.Property(customer => customer.Address).HasMaxLength(500);
            entity.Property(customer => customer.NationalIdNumber).HasMaxLength(80);
            entity.Property(customer => customer.Occupation).HasMaxLength(120);
            entity.Property(customer => customer.EmployerName).HasMaxLength(160);
            entity.Property(customer => customer.MonthlyIncome).HasPrecision(18, 2);
            entity.Property(customer => customer.RejectionReason).HasMaxLength(500);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.Property(account => account.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(account => account.AccountNumber).IsUnique();
            entity.Property(account => account.AccountNumber).HasMaxLength(30);
            entity.Property(account => account.AccountType).HasMaxLength(40);
            entity.Property(account => account.Currency).HasMaxLength(3).HasDefaultValue("GHS");
            entity.Property(account => account.Balance).HasPrecision(18, 2);
            entity.Property(account => account.DailyTransferLimit).HasPrecision(18, 2).HasDefaultValue(10000m);
            entity.Property(account => account.DailyWithdrawalLimit).HasPrecision(18, 2).HasDefaultValue(5000m);
        });

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.Property(transaction => transaction.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.BalanceAfterTransaction).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Description).HasMaxLength(500);
            entity.Property(transaction => transaction.ReferenceNumber).HasMaxLength(50);
            entity.Property(transaction => transaction.CounterpartyName).HasMaxLength(220);
            entity.Property(transaction => transaction.CounterpartyAccountNumber).HasMaxLength(30);
            entity.Property(transaction => transaction.CounterpartyAccountType).HasMaxLength(40);
            entity.Property(transaction => transaction.CounterpartyEmail).HasMaxLength(256);
            entity.Property(transaction => transaction.TransactionType).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<SystemAdmin>(entity =>
        {
            entity.Property(admin => admin.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(admin => admin.Email).IsUnique();
            entity.Property(admin => admin.FullName).HasMaxLength(160);
            entity.Property(admin => admin.Email).HasMaxLength(256);
            entity.Property(admin => admin.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<KycDocument>(entity =>
        {
            entity.Property(document => document.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(document => document.DocumentType).HasMaxLength(80);
            entity.Property(document => document.OriginalFileName).HasMaxLength(260);
            entity.Property(document => document.StoredFileName).HasMaxLength(260);
            entity.Property(document => document.ContentType).HasMaxLength(120);
        });

        modelBuilder.Entity<SpendingControl>(entity =>
        {
            entity.Property(control => control.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(control => control.CustomerId).IsUnique();
            entity.Property(control => control.MonthlySpendLimit).HasPrecision(18, 2);
            entity.Property(control => control.SingleTransactionLimit).HasPrecision(18, 2);
            entity.Property(control => control.SavingsTarget).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CustomerNotification>(entity =>
        {
            entity.Property(notification => notification.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(notification => notification.Type).HasMaxLength(60);
            entity.Property(notification => notification.Title).HasMaxLength(160);
            entity.Property(notification => notification.Message).HasMaxLength(500);
            entity.HasIndex(notification => new { notification.CustomerId, notification.CreatedAtUtc });
        });
    }
}
