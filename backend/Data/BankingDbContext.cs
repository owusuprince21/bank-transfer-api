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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(customer => customer.Email).IsUnique();
            entity.Property(customer => customer.Email).HasMaxLength(256);
            entity.Property(customer => customer.PasswordHash).HasMaxLength(500);
            entity.Property(customer => customer.FirstName).HasMaxLength(100);
            entity.Property(customer => customer.LastName).HasMaxLength(100);
            entity.Property(customer => customer.PhoneNumber).HasMaxLength(30);
            entity.Property(customer => customer.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.Property(account => account.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(account => account.AccountNumber).IsUnique();
            entity.Property(account => account.AccountNumber).HasMaxLength(30);
            entity.Property(account => account.AccountType).HasMaxLength(40);
            entity.Property(account => account.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.Property(transaction => transaction.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.BalanceAfterTransaction).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Description).HasMaxLength(500);
            entity.Property(transaction => transaction.ReferenceNumber).HasMaxLength(50);
            entity.Property(transaction => transaction.TransactionType).HasConversion<string>().HasMaxLength(30);
        });
    }
}
