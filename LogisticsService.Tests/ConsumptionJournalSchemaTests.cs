using LogisticsService.Data;
using LogisticsService.Migrations;
using LogisticsService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace LogisticsService.Tests;

public class ConsumptionJournalSchemaTests
{
    [Fact]
    public void Migration_CreatesJournalTriggerBaselineAndSafeDropOrder()
    {
        var migration = new AddConsumptionChangeJournal();
        var table = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        Assert.Equal("ConsumptionChange", table.Name);
        Assert.Empty(table.ForeignKeys);
        Assert.Contains(table.Columns, c => c.Name == "BeforeConsumedDate" && c.IsNullable);
        Assert.Contains(table.Columns, c => c.Name == "ChangeId" && c.ClrType == typeof(long));
        var sql = migration.UpOperations.OfType<SqlOperation>().ToArray();
        Assert.Equal(2, sql.Length);
        Assert.Contains("AFTER INSERT, UPDATE, DELETE", sql[0].Sql);
        Assert.Contains("'B'", sql[1].Sql);
        Assert.IsType<SqlOperation>(migration.DownOperations[0]);
        Assert.IsType<DropTableOperation>(migration.DownOperations[1]);
        Assert.NotNull(migration.TargetModel.FindEntityType(typeof(ConsumptionChange).FullName!));
    }

    [Fact]
    public void SqlServerMapping_DisablesIncompatibleOutputClauseForTriggeredTable()
    {
        using var db = new LogisticsDbContext(new DbContextOptionsBuilder<LogisticsDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options);
        var record = db.Model.FindEntityType(typeof(ConsumptionRecord))!;
        Assert.False(record.IsSqlOutputClauseUsed());
        var journal = db.Model.FindEntityType(typeof(ConsumptionChange))!;
        Assert.Empty(journal.GetForeignKeys());
        Assert.DoesNotContain(journal.GetProperties(), p => p.Name is "ConsumedBy" or "WardId" or "ItemName");
    }
}
