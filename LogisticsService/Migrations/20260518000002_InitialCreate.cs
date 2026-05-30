using System;
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
            migrationBuilder.CreateTable(
                name: "ConsumptionRecord",
                columns: table => new
                {
                    ConsumptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    WardId = table.Column<int>(type: "int", nullable: true),
                    QuantityConsumed = table.Column<int>(type: "int", nullable: false),
                    ConsumedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConsumedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionRecord", x => x.ConsumptionId);
                });

            migrationBuilder.CreateTable(
                name: "TransferOrder",
                columns: table => new
                {
                    TransferOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromFacilityId = table.Column<int>(type: "int", nullable: false),
                    FromFacilityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToFacilityId = table.Column<int>(type: "int", nullable: false),
                    ToFacilityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrder", x => x.TransferOrderId);
                });

            migrationBuilder.CreateTable(
                name: "TransferOrderItem",
                columns: table => new
                {
                    TransferOrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferOrderId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrderItem", x => x.TransferOrderItemId);
                    table.ForeignKey(
                        name: "FK_TransferOrderItem_TransferOrder",
                        column: x => x.TransferOrderId,
                        principalTable: "TransferOrder",
                        principalColumn: "TransferOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItem_TransferOrderId",
                table: "TransferOrderItem",
                column: "TransferOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TransferOrderItem");
            migrationBuilder.DropTable(name: "TransferOrder");
            migrationBuilder.DropTable(name: "ConsumptionRecord");
        }
    }
}
