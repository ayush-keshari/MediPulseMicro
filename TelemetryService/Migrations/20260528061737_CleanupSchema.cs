using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelemetryService.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('TelemetryRecord','ExcursionNote') IS NOT NULL ALTER TABLE [TelemetryRecord] DROP COLUMN [ExcursionNote]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcursionNote",
                table: "TelemetryRecord",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
