using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsService.Migrations
{
    /// <inheritdoc />
    public partial class AddToStorageZoneToTransferOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('TransferOrderItem','ToStorageZoneId') IS NULL " +
                "ALTER TABLE [TransferOrderItem] ADD [ToStorageZoneId] int NOT NULL DEFAULT 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH('TransferOrderItem','ToStorageZoneId') IS NOT NULL " +
                "ALTER TABLE [TransferOrderItem] DROP COLUMN [ToStorageZoneId]");
        }
    }
}
