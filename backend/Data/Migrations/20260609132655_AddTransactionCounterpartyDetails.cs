using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiDemo.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionCounterpartyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CounterpartyAccountNumber",
                table: "BankTransactions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyAccountType",
                table: "BankTransactions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyEmail",
                table: "BankTransactions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterpartyName",
                table: "BankTransactions",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE transactionRecord
                SET
                    CounterpartyAccountNumber = counterpartyAccount.AccountNumber,
                    CounterpartyAccountType = counterpartyAccount.AccountType,
                    CounterpartyEmail = counterpartyCustomer.Email,
                    CounterpartyName = LTRIM(RTRIM(CONCAT(counterpartyCustomer.FirstName, ' ', counterpartyCustomer.LastName)))
                FROM BankTransactions transactionRecord
                INNER JOIN BankTransactions counterpartyTransaction
                    ON counterpartyTransaction.ReferenceNumber = transactionRecord.ReferenceNumber
                    AND counterpartyTransaction.BankAccountId <> transactionRecord.BankAccountId
                INNER JOIN BankAccounts counterpartyAccount
                    ON counterpartyAccount.Id = counterpartyTransaction.BankAccountId
                INNER JOIN Customers counterpartyCustomer
                    ON counterpartyCustomer.Id = counterpartyAccount.CustomerId
                WHERE transactionRecord.TransactionType IN ('TransferSent', 'TransferReceived')
                    AND transactionRecord.CounterpartyAccountNumber IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CounterpartyAccountNumber",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "CounterpartyAccountType",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "CounterpartyEmail",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "CounterpartyName",
                table: "BankTransactions");
        }
    }
}
