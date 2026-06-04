using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementService.Migrations
{
    /// <inheritdoc />
    public partial class CleanUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop Remarks column if it exists (idempotent — safe on any DB state)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'Receipt') AND name = 'Remarks'
                )
                BEGIN
                    ALTER TABLE [Receipt] DROP COLUMN [Remarks];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'Receipt') AND name = 'Remarks'
                )
                BEGIN
                    ALTER TABLE [Receipt] ADD [Remarks] nvarchar(500) NULL;
                END
            ");
        }
    }
}
