using LogisticsService.Models;
using LogisticsService.Services;

namespace LogisticsService.Tests;

public class FefoStockAllocationServiceTests
{
    private readonly FefoStockAllocationService _allocator = new();

    [Fact]
    public void CreatePlan_ConsumesEarliestExpiryLotsFirst()
    {
        var late = new InventoryPosition { PositionId = 2, Quantity = 10, ExpiryDate = new DateTime(2028, 1, 1) };
        var early = new InventoryPosition { PositionId = 1, Quantity = 4, ExpiryDate = new DateTime(2027, 1, 1) };

        var plan = _allocator.CreatePlan(new[] { late, early }, 7);

        Assert.Equal(new[] { 1, 2 }, plan.Allocations.Select(a => a.Position.PositionId));
        Assert.Equal(new[] { 4, 3 }, plan.Allocations.Select(a => a.Quantity));
        Assert.Equal(0, plan.UnallocatedQuantity);
        Assert.Same(early, plan.SourceTemplate);
    }

    [Fact]
    public void CreatePlan_ReportsInsufficientStockWithoutOverAllocating()
    {
        var source = new InventoryPosition { PositionId = 1, Quantity = 3, ExpiryDate = DateTime.UtcNow };

        var plan = _allocator.CreatePlan(new[] { source }, 5);

        Assert.Single(plan.Allocations);
        Assert.Equal(3, plan.Allocations[0].Quantity);
        Assert.Equal(2, plan.UnallocatedQuantity);
    }

    [Fact]
    public void CreatePlan_UsesPositionIdAsStableTieBreaker()
    {
        var first = new InventoryPosition { PositionId = 10, Quantity = 1, ExpiryDate = new DateTime(2027, 1, 1) };
        var second = new InventoryPosition { PositionId = 5, Quantity = 1, ExpiryDate = new DateTime(2027, 1, 1) };

        var plan = _allocator.CreatePlan(new[] { first, second }, 1);

        Assert.Equal(5, plan.Allocations.Single().Position.PositionId);
    }
}
