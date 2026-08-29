using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStock",
                table: "ReplenishmentPlan");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "ReplenishmentPlan");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ReplenishmentPlan");

            migrationBuilder.DropColumn(
                name: "OrderedDate",
                table: "ReplenishmentPlan");

            migrationBuilder.DropColumn(
                name: "SafetyStockLevel",
                table: "ReplenishmentPlan");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "RecallAction");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RecallAction");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "RecallAction");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "RecallAction");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InventoryPositions");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "InventoryPositions");

            migrationBuilder.DropColumn(
                name: "AvgDailyUsage",
                table: "Forecast");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "Forecast");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Forecast");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ExceptionEvent");

            migrationBuilder.DropColumn(
                name: "ResolvedDate",
                table: "ExceptionEvent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStock",
                table: "ReplenishmentPlan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "ReplenishmentPlan",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ReplenishmentPlan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderedDate",
                table: "ReplenishmentPlan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SafetyStockLevel",
                table: "ReplenishmentPlan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "RecallAction",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RecallAction",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "RecallAction",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "RecallAction",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Items",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Items",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InventoryPositions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedDate",
                table: "InventoryPositions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "AvgDailyUsage",
                table: "Forecast",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Forecast",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Forecast",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ExceptionEvent",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedDate",
                table: "ExceptionEvent",
                type: "datetime2",
                nullable: true);
        }
    }
}
