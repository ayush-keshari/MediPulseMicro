using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionAndReplenishment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExceptionEvent",
                columns: table => new
                {
                    ExceptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FacilityId = table.Column<int>(type: "int", nullable: true),
                    LotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    DetectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionEvent", x => x.ExceptionId);
                });

            migrationBuilder.CreateTable(
                name: "Forecast",
                columns: table => new
                {
                    ForecastId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ForecastQuantity = table.Column<int>(type: "int", nullable: false),
                    AvgDailyUsage = table.Column<double>(type: "float", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecast", x => x.ForecastId);
                });

            migrationBuilder.CreateTable(
                name: "ReplenishmentPlan",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    SafetyStockLevel = table.Column<int>(type: "int", nullable: false),
                    SuggestedOrderQty = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentPlan", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "RecallAction",
                columns: table => new
                {
                    RecallActionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExceptionId = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ActionDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecallAction", x => x.RecallActionId);
                    table.ForeignKey(
                        name: "FK_RecallAction_ExceptionEvent_ExceptionId",
                        column: x => x.ExceptionId,
                        principalTable: "ExceptionEvent",
                        principalColumn: "ExceptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionEvent_Type_Status",
                table: "ExceptionEvent",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Forecast_FacilityId_ItemId_Period",
                table: "Forecast",
                columns: new[] { "FacilityId", "ItemId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_RecallAction_ExceptionId",
                table: "RecallAction",
                column: "ExceptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentPlan_FacilityId_Status",
                table: "ReplenishmentPlan",
                columns: new[] { "FacilityId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Forecast");

            migrationBuilder.DropTable(
                name: "RecallAction");

            migrationBuilder.DropTable(
                name: "ReplenishmentPlan");

            migrationBuilder.DropTable(
                name: "ExceptionEvent");
        }
    }
}
