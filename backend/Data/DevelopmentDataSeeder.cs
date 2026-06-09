using ApiDemo.Models;
using ApiDemo.Services;
using Microsoft.EntityFrameworkCore;

namespace ApiDemo.Data;

public static class DevelopmentDataSeeder
{
    public const string DemoEmail = "demo.customer@example.com";
    public const string DemoPassword = "Password123!";
    public const string AdminEmail = "admin@apidemo.test";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        await dbContext.Database.MigrateAsync();

        var demoCustomer = await dbContext.Customers
            .Include(existingCustomer => existingCustomer.Accounts)
            .FirstOrDefaultAsync(existingCustomer => existingCustomer.Email == DemoEmail);

        if (demoCustomer is null)
        {
            demoCustomer = new Customer
            {
                FirstName = "Demo",
                LastName = "Customer",
                Email = DemoEmail,
                PasswordHash = PasswordHasher.Hash(DemoPassword),
                Status = CustomerStatus.Active,
                PhoneNumber = "+15550109999",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Address = "100 Demo Street, New York, NY"
            };

            var account = new BankAccount
            {
                Customer = demoCustomer,
                AccountNumber = $"10{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
                AccountType = "Savings",
                Balance = 5000.00m
            };

            account.Transactions.Add(new BankTransaction
            {
                BankAccount = account,
                TransactionType = TransactionType.Deposit,
                Amount = 5000.00m,
                BalanceAfterTransaction = 5000.00m,
                Description = "Demo opening deposit",
                ReferenceNumber = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100000, 999999)}"
            });

            demoCustomer.Accounts.Add(account);
            dbContext.Customers.Add(demoCustomer);
        }
        else if (string.IsNullOrWhiteSpace(demoCustomer.PasswordHash))
        {
            demoCustomer.PasswordHash = PasswordHasher.Hash(DemoPassword);
            demoCustomer.Status = CustomerStatus.Active;
        }
        else if (demoCustomer.Status == CustomerStatus.PendingApproval)
        {
            demoCustomer.Status = CustomerStatus.Active;
        }

        var existingCustomers = await dbContext.Customers
            .Include(customer => customer.Accounts)
            .Where(customer => customer.Email != DemoEmail)
            .ToListAsync();

        foreach (var customer in existingCustomers)
        {
            if (string.IsNullOrWhiteSpace(customer.PasswordHash))
            {
                customer.PasswordHash = PasswordHasher.Hash(DemoPassword);
            }

            if (customer.Status == CustomerStatus.Active && customer.Accounts.Count == 0)
            {
                customer.Accounts.Add(new BankAccount
                {
                    Customer = customer,
                    AccountNumber = $"10{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(1000, 9999)}",
                    AccountType = "Savings",
                    Balance = 0.00m
                });
            }
        }

        var admin = await dbContext.SystemAdmins.FirstOrDefaultAsync(existingAdmin => existingAdmin.Email == AdminEmail);
        if (admin is null)
        {
            dbContext.SystemAdmins.Add(new SystemAdmin
            {
                FullName = "System Admin",
                Email = AdminEmail,
                PasswordHash = PasswordHasher.Hash(AdminPassword)
            });
        }
        else if (!PasswordHasher.Verify(AdminPassword, admin.PasswordHash))
        {
            admin.PasswordHash = PasswordHasher.Hash(AdminPassword);
        }

        await dbContext.SaveChangesAsync();
    }
}
