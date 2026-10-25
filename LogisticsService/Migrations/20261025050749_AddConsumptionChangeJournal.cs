using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsService.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumptionChangeJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsumptionChange",
                columns: table => new
                {
                    ChangeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumptionId = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    BeforeFacilityId = table.Column<int>(type: "int", nullable: true),
                    BeforeItemId = table.Column<int>(type: "int", nullable: true),
                    BeforeConsumedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BeforeQuantity = table.Column<int>(type: "int", nullable: true),
                    AfterFacilityId = table.Column<int>(type: "int", nullable: true),
                    AfterItemId = table.Column<int>(type: "int", nullable: true),
                    AfterConsumedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AfterQuantity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumptionChange", x => x.ChangeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumptionChange_ConsumptionId_ChangeId",
                table: "ConsumptionChange",
                columns: new[] { "ConsumptionId", "ChangeId" });

            migrationBuilder.Sql("""
                CREATE TRIGGER dbo.TR_ConsumptionRecord_AnalyticsChanges
                ON dbo.ConsumptionRecord
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    INSERT dbo.ConsumptionChange
                        (ConsumptionId, Operation, BeforeFacilityId, BeforeItemId,
                         BeforeConsumedDate, BeforeQuantity, AfterFacilityId, AfterItemId,
                         AfterConsumedDate, AfterQuantity)
                    SELECT COALESCE(i.ConsumptionId, d.ConsumptionId),
                           CASE WHEN d.ConsumptionId IS NULL THEN 'I'
                                WHEN i.ConsumptionId IS NULL THEN 'D' ELSE 'U' END,
                           d.FacilityId, d.ItemId, d.ConsumedDate, d.QuantityConsumed,
                           i.FacilityId, i.ItemId, i.ConsumedDate, i.QuantityConsumed
                    FROM inserted i
                    FULL OUTER JOIN deleted d ON i.ConsumptionId = d.ConsumptionId
                    WHERE i.ConsumptionId IS NULL OR d.ConsumptionId IS NULL
                       OR i.FacilityId <> d.FacilityId OR i.ItemId <> d.ItemId
                       OR i.ConsumedDate <> d.ConsumedDate OR i.QuantityConsumed <> d.QuantityConsumed;
                END
                """);

            // Existing rows form a baseline, not invented historical events.
            migrationBuilder.Sql("""
                INSERT dbo.ConsumptionChange
                    (ConsumptionId, Operation, AfterFacilityId, AfterItemId, AfterConsumedDate, AfterQuantity)
                SELECT ConsumptionId, 'B', FacilityId, ItemId, ConsumedDate, QuantityConsumed
                FROM dbo.ConsumptionRecord;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.TR_ConsumptionRecord_AnalyticsChanges;");
            migrationBuilder.DropTable(
                name: "ConsumptionChange");
        }
    }
}
