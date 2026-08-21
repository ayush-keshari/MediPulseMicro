using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS8981

namespace InventoryService.Migrations
{
    /// <inheritdoc />
    public partial class create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StorageRequirement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SafetyStock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "InventoryPositions",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    LotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    StorageZoneId = table.Column<int>(type: "int", nullable: false),
                    SafetyStock = table.Column<int>(type: "int", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryPositions", x => x.PositionId);
                    table.ForeignKey(
                        name: "FK_InventoryPositions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryPositions_ItemId",
                table: "InventoryPositions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCode",
                table: "Items",
                column: "ItemCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryPositions");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}

#pragma warning restore CS8981
