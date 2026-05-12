using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityService.Migrations
{
    /// <inheritdoc />
    public partial class Change : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facility",
                columns: table => new
                {
                    FacilityID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Facility__5FB08B94A6B7EFA8", x => x.FacilityID);
                });

            migrationBuilder.CreateTable(
                name: "StorageZone",
                columns: table => new
                {
                    ZoneID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityID = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemperatureProfile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Capacity = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StorageZ__601667959C48E490", x => x.ZoneID);
                    table.ForeignKey(
                        name: "FK__StorageZo__Facil__52593CB8",
                        column: x => x.FacilityID,
                        principalTable: "Facility",
                        principalColumn: "FacilityID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageZone_FacilityID",
                table: "StorageZone",
                column: "FacilityID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageZone");

            migrationBuilder.DropTable(
                name: "Facility");
        }
    }
}
