using LogisticsService.Data;
using LogisticsService.DTOs;
using LogisticsService.Models;
using LogisticsService.Services;
using Microsoft.EntityFrameworkCore;

namespace LogisticsService.Tests;

public class ConsumptionBehaviorTests
{
    private static LogisticsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LogisticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new LogisticsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateConsumptionAsync_DeductsStockUsingFefo()
    {
        await using var context = CreateContext();
        context.InventoryPositions.AddRange(
            new InventoryPosition
            {
                ItemId = 1,
                FacilityId = 1,
                StorageZoneId = 1,
                LotId = "EARLY",
                ExpiryDate = new DateTime(2027, 1, 1),
                Quantity = 10,
                SafetyStock = 2
            },
            new InventoryPosition
            {
                ItemId = 1,
                FacilityId = 1,
                StorageZoneId = 1,
                LotId = "LATE",
                ExpiryDate = new DateTime(2028, 1, 1),
                Quantity = 20,
                SafetyStock = 2
            });
        await context.SaveChangesAsync();
        var service = new LogisticsServiceImpl(context);

        var result = await service.CreateConsumptionAsync(new CreateConsumptionRequest
        {
            FacilityId = 1,
            WardId = 10,
            ItemId = 1,
            ItemName = "Insulin",
            QuantityConsumed = 15,
            ConsumedDate = DateTime.UtcNow,
            ConsumedBy = "nurse-1"
        });

        Assert.True(result);
        Assert.Equal(0, await context.InventoryPositions
            .Where(p => p.LotId == "EARLY").Select(p => p.Quantity).SingleAsync());
        Assert.Equal(15, await context.InventoryPositions
            .Where(p => p.LotId == "LATE").Select(p => p.Quantity).SingleAsync());
        Assert.Equal(1, await context.ConsumptionRecords.CountAsync());
    }

    [Fact]
    public async Task ConsumptionQueriesAndUpdates_ReturnMappedResults()
    {
        await using var context = CreateContext();
        var firstDate = new DateTime(2026, 1, 1);
        var secondDate = new DateTime(2026, 1, 2);
        context.ConsumptionRecords.AddRange(
            new ConsumptionRecord
            {
                FacilityId = 1,
                WardId = 10,
                ItemId = 1,
                ItemName = "Insulin",
                QuantityConsumed = 5,
                ConsumedDate = firstDate,
                ConsumedBy = "nurse-1"
            },
            new ConsumptionRecord
            {
                FacilityId = 2,
                WardId = 20,
                ItemId = 2,
                ItemName = "Saline",
                QuantityConsumed = 3,
                ConsumedDate = secondDate,
                ConsumedBy = "nurse-2"
            });
        await context.SaveChangesAsync();
        var service = new LogisticsServiceImpl(context);

        var all = (await service.GetAllConsumptionAsync()).ToList();
        var byFacility = await service.GetConsumptionByFacilityAsync(1);
        var byItem = await service.GetConsumptionByItemAsync(2);
        var byId = await service.GetConsumptionByIdAsync(all[0].ConsumptionId);

        Assert.Equal(2, all.Count);
        Assert.Equal(secondDate, all[0].ConsumedDate);
        Assert.Single(byFacility);
        Assert.Equal("Saline", Assert.Single(byItem).ItemName);
        Assert.Equal(all[0].ConsumptionId, byId!.ConsumptionId);

        var updated = await service.UpdateConsumptionAsync(all[0].ConsumptionId,
            new UpdateConsumptionRequest
            {
                QuantityConsumed = 8,
                ConsumedDate = new DateTime(2026, 1, 3),
                ConsumedBy = "nurse-3"
            });
        Assert.True(updated);
        Assert.Equal(8, (await service.GetConsumptionByIdAsync(all[0].ConsumptionId))!.QuantityConsumed);

        Assert.True(await service.DeleteConsumptionAsync(all[0].ConsumptionId));
        Assert.False(await service.DeleteConsumptionAsync(999));
        Assert.Null(await service.GetConsumptionByIdAsync(999));
    }
}
