using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Tests;

public class ReplenishmentServiceTests
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
    public async Task ForecastAndPlanQueries_ApplyFiltersAndMapValues()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Forecasts.AddRange(
            new Forecast { ItemId = 1, FacilityId = 4, Period = "2026-08", ForecastQuantity = 10, GeneratedDate = now },
            new Forecast { ItemId = 2, FacilityId = 5, Period = "2026-08", ForecastQuantity = 20, GeneratedDate = now.AddMinutes(-1) });
        context.ReplenishmentPlans.AddRange(
            new ReplenishmentPlan { ItemId = 1, FacilityId = 4, SuggestedOrderQty = 15, Priority = "High", Status = "Pending", GeneratedDate = now },
            new ReplenishmentPlan { ItemId = 2, FacilityId = 5, SuggestedOrderQty = 5, Priority = "Low", Status = "Ordered", GeneratedDate = now.AddMinutes(-1) });
        await context.SaveChangesAsync();
        var service = new ReplenishmentServiceImpl(context);

        var forecast = Assert.Single(await service.GetForecastsAsync(4, 1));
        var plan = Assert.Single(await service.GetPlansAsync(4, "Pending", "High"));

        Assert.Equal(10, forecast.ForecastQuantity);
        Assert.Equal(15, plan.SuggestedOrderQty);
        Assert.NotNull(await service.GetForecastByIdAsync(forecast.ForecastId));
        Assert.NotNull(await service.GetPlanByIdAsync(plan.PlanId));
        Assert.Null(await service.GetForecastByIdAsync(999));
        Assert.Null(await service.GetPlanByIdAsync(999));
    }

    [Fact]
    public async Task PlanLifecycle_UpdatesAndDeletesExistingPlans()
    {
        await using var context = CreateContext();
        var plan = new ReplenishmentPlan { ItemId = 1, FacilityId = 2, SuggestedOrderQty = 8 };
        context.ReplenishmentPlans.Add(plan);
        await context.SaveChangesAsync();
        var service = new ReplenishmentServiceImpl(context);

        Assert.False(await service.UpdatePlanStatusAsync(999, new UpdatePlanStatusRequest { Status = "Ordered" }));
        Assert.True(await service.UpdatePlanStatusAsync(plan.PlanId, new UpdatePlanStatusRequest { Status = "Fulfilled" }));
        Assert.Equal("Fulfilled", (await context.ReplenishmentPlans.FindAsync(plan.PlanId))!.Status);
        Assert.False(await service.DeletePlanAsync(999));
        Assert.True(await service.DeletePlanAsync(plan.PlanId));
    }

    [Fact]
    public async Task Generate_CreatesForecastsAndLowStockPlans_AndReplacesPendingData()
    {
        await using var context = CreateContext();
        context.InventoryPositions.AddRange(
            new InventoryPosition { ItemId = 1, FacilityId = 8, Quantity = 0, SafetyStock = 5, LotId = "L1" },
            new InventoryPosition { ItemId = 1, FacilityId = 8, Quantity = 3, SafetyStock = 5, LotId = "L2" },
            new InventoryPosition { ItemId = 2, FacilityId = 8, Quantity = 20, SafetyStock = 5, LotId = "L3" },
            new InventoryPosition { ItemId = 3, FacilityId = 9, Quantity = 0, SafetyStock = 5, LotId = "OTHER" });
        var period = DateTime.UtcNow.ToString("yyyy-MM");
        context.Forecasts.AddRange(
            new Forecast { ItemId = 99, FacilityId = 8, Period = period, ForecastQuantity = 1 },
            new Forecast { ItemId = 98, FacilityId = 8, Period = period, ForecastQuantity = 2 });
        context.ReplenishmentPlans.AddRange(
            new ReplenishmentPlan { ItemId = 99, FacilityId = 8, Status = "Pending" },
            new ReplenishmentPlan { ItemId = 98, FacilityId = 8, Status = "Ordered" });
        await context.SaveChangesAsync();
        var service = new ReplenishmentServiceImpl(context);

        var result = await service.GenerateAsync(8);

        Assert.Equal(2, result.ForecastsCreated);
        Assert.Equal(1, result.PlansCreated);
        Assert.Equal(8, result.FacilityId);
        Assert.Equal(2, await context.Forecasts.CountAsync(f => f.FacilityId == 8));
        var plan = await context.ReplenishmentPlans.SingleAsync(p => p.ItemId == 1);
        Assert.Equal("Medium", plan.Priority);
        Assert.Equal(10, plan.SuggestedOrderQty);
        Assert.True(await context.ReplenishmentPlans.AnyAsync(p => p.ItemId == 98 && p.Status == "Ordered"));
        Assert.False(await context.ReplenishmentPlans.AnyAsync(p => p.ItemId == 99));
    }
}
