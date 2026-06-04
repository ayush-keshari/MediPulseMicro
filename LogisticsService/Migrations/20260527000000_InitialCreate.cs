using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TransferOrder')
                BEGIN
                    CREATE TABLE [TransferOrder] (
                        [TransferOrderId]   int IDENTITY(1,1) NOT NULL,
                        [FromFacilityId]    int NOT NULL,
                        [FromFacilityName]  nvarchar(100) NOT NULL,
                        [ToFacilityId]      int NOT NULL,
                        [ToFacilityName]    nvarchar(100) NOT NULL,
                        [RequestedBy]       nvarchar(100) NOT NULL,
                        [RequestedDate]     datetime2 NOT NULL,
                        [Status]            nvarchar(50) NOT NULL DEFAULT 'Draft',
                        CONSTRAINT [PK_TransferOrder] PRIMARY KEY ([TransferOrderId])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TransferOrderItem')
                BEGIN
                    CREATE TABLE [TransferOrderItem] (
                        [TransferOrderItemId]  int IDENTITY(1,1) NOT NULL,
                        [TransferOrderId]      int NOT NULL,
                        [ItemId]               int NOT NULL,
                        [ItemName]             nvarchar(150) NOT NULL,
                        [Quantity]             int NOT NULL,
                        CONSTRAINT [PK_TransferOrderItem] PRIMARY KEY ([TransferOrderItemId]),
                        CONSTRAINT [FK_TransferOrderItem_TransferOrder] FOREIGN KEY ([TransferOrderId])
                            REFERENCES [TransferOrder] ([TransferOrderId]) ON DELETE CASCADE
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TransferOrderItem_TransferOrderId')
                BEGIN
                    CREATE INDEX [IX_TransferOrderItem_TransferOrderId] ON [TransferOrderItem] ([TransferOrderId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConsumptionRecord')
                BEGIN
                    CREATE TABLE [ConsumptionRecord] (
                        [ConsumptionId]     int IDENTITY(1,1) NOT NULL,
                        [FacilityId]        int NOT NULL,
                        [WardId]            int NULL,
                        [ItemId]            int NOT NULL,
                        [ItemName]          nvarchar(150) NOT NULL,
                        [QuantityConsumed]  int NOT NULL,
                        [ConsumedDate]      datetime2 NOT NULL,
                        [ConsumedBy]        nvarchar(100) NOT NULL,
                        CONSTRAINT [PK_ConsumptionRecord] PRIMARY KEY ([ConsumptionId])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TransferOrderItem') DROP TABLE [TransferOrderItem];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TransferOrder') DROP TABLE [TransferOrder];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConsumptionRecord') DROP TABLE [ConsumptionRecord];");
        }
    }
}
