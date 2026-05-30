using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementService.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('Receipt','Remarks') IS NOT NULL ALTER TABLE [Receipt] DROP COLUMN [Remarks]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Receipt",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
