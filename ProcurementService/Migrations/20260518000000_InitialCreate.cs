using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Supplier')
                BEGIN
                    CREATE TABLE [Supplier] (
                        [SupplierID] int IDENTITY(1,1) NOT NULL,
                        [Name] nvarchar(100) NOT NULL,
                        [SupplierType] nvarchar(50) NULL,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'Active',
                        CONSTRAINT [PK_Supplier] PRIMARY KEY ([SupplierID])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrder')
                BEGIN
                    CREATE TABLE [PurchaseOrder] (
                        [POID] int IDENTITY(1,1) NOT NULL,
                        [SupplierID] int NOT NULL,
                        [OrderDate] datetime2 NOT NULL,
                        [ExpectedDeliveryDate] datetime2 NULL,
                        [Status] nvarchar(50) NOT NULL DEFAULT 'Draft',
                        [Notes] nvarchar(500) NULL,
                        CONSTRAINT [PK_PurchaseOrder] PRIMARY KEY ([POID]),
                        CONSTRAINT [FK_PurchaseOrder_Supplier] FOREIGN KEY ([SupplierID])
                            REFERENCES [Supplier] ([SupplierID]) ON DELETE NO ACTION
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PurchaseOrder_SupplierID')
                BEGIN
                    CREATE INDEX [IX_PurchaseOrder_SupplierID] ON [PurchaseOrder] ([SupplierID]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Receipt')
                BEGIN
                    CREATE TABLE [Receipt] (
                        [ReceiptID] int IDENTITY(1,1) NOT NULL,
                        [POID] int NOT NULL,
                        [SupplierLot] nvarchar(100) NULL,
                        [ReceivedDate] datetime2 NOT NULL,
                        [ReceivedBy] nvarchar(100) NOT NULL,
                        [QualityStatus] nvarchar(50) NOT NULL DEFAULT 'Accepted',
                        [QuantityReceived] int NOT NULL,
                        [Remarks] nvarchar(500) NULL,
                        CONSTRAINT [PK_Receipt] PRIMARY KEY ([ReceiptID]),
                        CONSTRAINT [FK_Receipt_PurchaseOrder] FOREIGN KEY ([POID])
                            REFERENCES [PurchaseOrder] ([POID]) ON DELETE CASCADE
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Receipt_POID')
                BEGIN
                    CREATE INDEX [IX_Receipt_POID] ON [Receipt] ([POID]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Receipt') DROP TABLE [Receipt];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrder') DROP TABLE [PurchaseOrder];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Supplier') DROP TABLE [Supplier];");
        }
    }
}
