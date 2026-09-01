using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Tests;

public class ExceptionServiceTests
{
    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new InventoryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateAndFilterExceptions_ReturnsMappedActions()
    {
        await using var context = CreateContext();
        var service = new ExceptionServiceImpl(context);
        var request = new CreateExceptionRequest
        {
            Type = "Recall",
            ReferenceType = "Lot",
            ReferenceId = 7,
            ItemId = 2,
            ItemName = "Vaccine",
            FacilityId = 3,
            LotId = "LOT-7",
            Severity = "High"
        };

        Assert.True(await service.CreateAsync(request));
        var exception = await context.ExceptionEvents.SingleAsync();
        context.RecallActions.Add(new RecallAction
        {
            ExceptionId = exception.ExceptionId,
            OwnerId = "user-1",
            ActionDescription = "Quarantine lot",
            DueDate = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var result = (await service.GetAllAsync("Recall", "Open", "High")).ToList();

        var mapped = Assert.Single(result);
        Assert.Equal("Vaccine", mapped.ItemName);
        Assert.Equal("LOT-7", mapped.LotId);
        Assert.Equal("Quarantine lot", Assert.Single(mapped.Actions).ActionDescription);
        Assert.NotNull(await service.GetByIdAsync(exception.ExceptionId));
        Assert.Null(await service.GetByIdAsync(999));
    }

    [Fact]
    public async Task ExceptionStatusAndDelete_ReturnExpectedResults()
    {
        await using var context = CreateContext();
        var service = new ExceptionServiceImpl(context);
        var exception = new ExceptionEvent
        {
            Type = "Stockout",
            ReferenceType = "InventoryPosition",
            ReferenceId = 1
        };
        context.ExceptionEvents.Add(exception);
        await context.SaveChangesAsync();

        Assert.False(await service.UpdateStatusAsync(999, new UpdateExceptionStatusRequest { Status = "Resolved" }));
        Assert.True(await service.UpdateStatusAsync(exception.ExceptionId, new UpdateExceptionStatusRequest { Status = "Resolved" }));
        Assert.Equal("Resolved", (await context.ExceptionEvents.FindAsync(exception.ExceptionId))!.Status);
        Assert.False(await service.DeleteAsync(999));
        Assert.True(await service.DeleteAsync(exception.ExceptionId));
    }

    [Fact]
    public async Task Detect_CreatesStockoutAndExpiryAlerts_Once()
    {
        await using var context = CreateContext();
        var item = new Item { ItemCode = "MED-1", Name = "Vaccine", Category = "Pharma", Unit = "Vial" };
        context.Items.Add(item);
        await context.SaveChangesAsync();
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = item.ItemId,
                Item = item,
                PositionId = 10,
                LotId = "EMPTY",
                FacilityId = 4,
                StorageZoneId = 1,
                Quantity = 0,
                SafetyStock = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(60)
            },
            new InventoryPosition
            {
                ItemId = item.ItemId,
                Item = item,
                PositionId = 11,
                LotId = "EXPIRING",
                FacilityId = 4,
                StorageZoneId = 1,
                Quantity = 3,
                SafetyStock = 2,
                ExpiryDate = DateTime.UtcNow.AddDays(3)
            });
        await context.SaveChangesAsync();
        var service = new ExceptionServiceImpl(context);

        var first = await service.DetectAsync(4, 7);
        var second = await service.DetectAsync(4, 7);

        Assert.Equal(1, first.StockoutCount);
        Assert.Equal(1, first.ExpiryCount);
        Assert.Equal(2, first.TotalCreated);
        Assert.Equal(0, second.TotalCreated);
        Assert.Equal(2, await context.ExceptionEvents.CountAsync());
        Assert.All(await context.ExceptionEvents.ToListAsync(), e => Assert.Equal("High", e.Severity));
    }

    [Fact]
    public async Task RecallActionLifecycle_UpdatesParentAndSupportsMissingIds()
    {
        await using var context = CreateContext();
        var service = new ExceptionServiceImpl(context);
        var exception = new ExceptionEvent { Type = "Recall", ReferenceType = "Lot", ReferenceId = 1 };
        context.ExceptionEvents.Add(exception);
        await context.SaveChangesAsync();

        var request = new CreateRecallActionRequest
        {
            ExceptionId = exception.ExceptionId,
            OwnerId = "owner-1",
            ActionDescription = "Review",
            DueDate = DateTime.UtcNow.AddDays(2)
        };
        Assert.True(await service.CreateActionAsync(request));
        Assert.Equal("InProgress", (await context.ExceptionEvents.FindAsync(exception.ExceptionId))!.Status);
        var action = await context.RecallActions.SingleAsync();
        Assert.Equal("Pending", action.Status);
        Assert.NotNull(await service.GetActionByIdAsync(action.RecallActionId));
        Assert.Empty(await service.GetActionsAsync(999));

        Assert.True(await service.UpdateActionAsync(action.RecallActionId, new UpdateRecallActionRequest
        {
            ActionDescription = "Complete review",
            Status = "Completed",
            DueDate = DateTime.UtcNow.AddDays(3)
        }));
        Assert.False(await service.UpdateActionAsync(999, new UpdateRecallActionRequest { Status = "Completed" }));
        Assert.Equal("Completed", (await context.RecallActions.FindAsync(action.RecallActionId))!.Status);
        Assert.True(await service.DeleteActionAsync(action.RecallActionId));
        Assert.False(await service.DeleteActionAsync(action.RecallActionId));
    }
}
