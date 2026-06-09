using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiDemo.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomerPendingApprovalDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @constraintName sysname;

                SELECT @constraintName = default_constraints.name
                FROM sys.default_constraints
                INNER JOIN sys.columns
                    ON columns.default_object_id = default_constraints.object_id
                INNER JOIN sys.tables
                    ON tables.object_id = columns.object_id
                WHERE tables.name = 'Customers'
                    AND columns.name = 'Status';

                IF @constraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [Customers] DROP CONSTRAINT [' + @constraintName + ']');
                END;

                ALTER TABLE [Customers]
                ADD CONSTRAINT [DF_Customers_Status] DEFAULT N'PendingApproval' FOR [Status];

                UPDATE [Customers]
                SET [Status] = N'PendingApproval'
                WHERE [Status] = N'Active'
                    AND [ApprovedAtUtc] IS NULL
                    AND [Email] <> N'demo.customer@example.com';
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @constraintName sysname;

                SELECT @constraintName = default_constraints.name
                FROM sys.default_constraints
                INNER JOIN sys.columns
                    ON columns.default_object_id = default_constraints.object_id
                INNER JOIN sys.tables
                    ON tables.object_id = columns.object_id
                WHERE tables.name = 'Customers'
                    AND columns.name = 'Status';

                IF @constraintName IS NOT NULL
                BEGIN
                    EXEC('ALTER TABLE [Customers] DROP CONSTRAINT [' + @constraintName + ']');
                END;

                ALTER TABLE [Customers]
                ADD CONSTRAINT [DF_Customers_Status] DEFAULT N'Active' FOR [Status];
                """);

        }
    }
}
