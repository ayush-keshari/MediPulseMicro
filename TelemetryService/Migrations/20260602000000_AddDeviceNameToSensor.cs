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
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'SensorDevice') AND name = 'DeviceName'
                )
                BEGIN
                    ALTER TABLE [SensorDevice] ADD [DeviceName] nvarchar(100) NOT NULL DEFAULT '';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'SensorDevice') AND name = 'DeviceName'
                )
                BEGIN
                    ALTER TABLE [SensorDevice] DROP COLUMN [DeviceName];
                END
            ");
        }
    }
}
