using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public class ReplenishmentServiceImpl : IReplenishmentService
{
    private readonly InventoryDbContext _db;

    public ReplenishmentServiceImpl(InventoryDbContext db) => _db = db;

    // ── Forecasts ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<ForecastDto>> GetForecastsAsync(int? facilityId, int? itemId)
    {
        var query = _db.Forecasts.AsQueryable();
        if (facilityId.HasValue) query = query.Where(f => f.FacilityId == facilityId.Value);
        if (itemId.HasValue) query = query.Where(f => f.ItemId == itemId.Value);

        return await query
            .OrderByDescending(f => f.GeneratedDate)
            .Select(f => ToForecastDto(f))
            .ToListAsync();
    }

    public async Task<ForecastDto?> GetForecastByIdAsync(int id)
    {
        var f = await _db.Forecasts.FindAsync(id);
        return f == null ? null : ToForecastDto(f);
    }

    // ── Plans ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ReplenishmentPlanDto>> GetPlansAsync(
        int? facilityId, string? status, string? priority)
    {
        var query = _db.ReplenishmentPlans.AsQueryable();
        if (facilityId.HasValue) query = query.Where(p => p.FacilityId == facilityId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(priority)) query = query.Where(p => p.Priority == priority);

        return await query
            .OrderByDescending(p => p.GeneratedDate)
            .Select(p => ToPlanDto(p))
            .ToListAsync();
    }

    public async Task<ReplenishmentPlanDto?> GetPlanByIdAsync(int id)
    {
        var p = await _db.ReplenishmentPlans.FindAsync(id);
        return p == null ? null : ToPlanDto(p);
    }

    public async Task<bool> UpdatePlanStatusAsync(int id, UpdatePlanStatusRequest request)
    {
        var plan = await _db.ReplenishmentPlans.FindAsync(id);
        if (plan == null) return false;

        plan.Status = request.Status;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePlanAsync(int id)
    {
        var plan = await _db.ReplenishmentPlans.FindAsync(id);
        if (plan == null) return false;
        _db.ReplenishmentPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Generate ──────────────────────────────────────────────────────────

    public async Task<GenerateReplenishmentResult> GenerateAsync(int facilityId)
    {
        var result = new GenerateReplenishmentResult { FacilityId = facilityId };
        var now = DateTime.UtcNow;
        var period = now.ToString("yyyy-MM");
        var lookback = now.AddDays(-30);

        // 1. Get current stock grouped by item for this facility
        var stockByItem = await _db.InventoryPositions
            .Where(p => p.FacilityId == facilityId)
            .GroupBy(p => new { p.ItemId, p.SafetyStock })
            .Select(g => new
            {
                g.Key.ItemId,
                g.Key.SafetyStock,
                TotalQty = g.Sum(p => p.Quantity)
            })
            .ToListAsync();

        // 2. Get last 30 days consumption
        var consumptionByItem = await _db.ConsumptionRecords
            .Where(c => c.FacilityId == facilityId && c.ConsumedDate >= lookback)
            .GroupBy(c => c.ItemId)
            .Select(g => new { ItemId = g.Key, TotalConsumed = g.Sum(c => c.QuantityConsumed) })
            .ToListAsync();

        var consumptionMap = consumptionByItem.ToDictionary(c => c.ItemId, c => c.TotalConsumed);

        // 3. Remove existing Pending plans and forecasts for this facility/period
        var existingPending = await _db.ReplenishmentPlans
            .Where(p => p.FacilityId == facilityId && p.Status == "Pending")
            .ToListAsync();
        _db.ReplenishmentPlans.RemoveRange(existingPending);

        var existingForecasts = await _db.Forecasts
            .Where(f => f.FacilityId == facilityId && f.Period == period)
            .ToListAsync();
        _db.Forecasts.RemoveRange(existingForecasts);

        // 4. Generate forecast + plan per item
        foreach (var stock in stockByItem)
        {
            var totalConsumed = consumptionMap.GetValueOrDefault(stock.ItemId, 0);
            var forecastQty = (int)Math.Ceiling(totalConsumed / 30.0 * 30);

            _db.Forecasts.Add(new Forecast
            {
                ItemId = stock.ItemId,
                FacilityId = facilityId,
                Period = period,
                ForecastQuantity = forecastQty,
                GeneratedDate = now
            });
            result.ForecastsCreated++;

            if (stock.TotalQty <= stock.SafetyStock)
            {
                var suggestedQty = Math.Max(stock.SafetyStock * 2, forecastQty);
                var priority = stock.TotalQty == 0 ? "High"
                             : stock.TotalQty < stock.SafetyStock ? "Medium"
                             : "Low";

                _db.ReplenishmentPlans.Add(new ReplenishmentPlan
                {
                    ItemId = stock.ItemId,
                    FacilityId = facilityId,
                    SuggestedOrderQty = suggestedQty,
                    Priority = priority,
                    Status = "Pending",
                    GeneratedDate = now
                });
                result.PlansCreated++;
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static ForecastDto ToForecastDto(Forecast f) => new()
    {
        ForecastId = f.ForecastId,
        ItemId = f.ItemId,
        FacilityId = f.FacilityId,
        Period = f.Period,
        ForecastQuantity = f.ForecastQuantity,
        GeneratedDate = f.GeneratedDate
    };

    private static ReplenishmentPlanDto ToPlanDto(ReplenishmentPlan p) => new()
    {
        PlanId = p.PlanId,
        ItemId = p.ItemId,
        FacilityId = p.FacilityId,
        SuggestedOrderQty = p.SuggestedOrderQty,
        Priority = p.Priority,
        Status = p.Status,
        GeneratedDate = p.GeneratedDate
    };
}
