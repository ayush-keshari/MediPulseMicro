using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelemetryService.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceNameToSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "SensorDevice",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "SensorDevice");
        }
    }
}
