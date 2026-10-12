using LogisticsService.Models;

namespace LogisticsService.Services;

/// <summary>
/// Plans stock consumption using FEFO: the earliest-expiring lots are consumed first.
/// This class is deliberately persistence-free so the allocation rules can be tested independently.
/// </summary>
public sealed class FefoStockAllocationService
{
    public FefoAllocationPlan CreatePlan(IEnumerable<InventoryPosition> sourcePositions, int requestedQuantity)
    {
        if (requestedQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedQuantity), "Requested quantity must be positive.");

        var orderedPositions = sourcePositions
            .Where(position => position.Quantity > 0)
            .OrderBy(position => position.ExpiryDate)
            .ThenBy(position => position.PositionId)
            .ToList();

        var remaining = requestedQuantity;
        var allocations = new List<FefoLotAllocation>();

        foreach (var position in orderedPositions)
        {
            if (remaining == 0)
                break;

            var allocatedQuantity = Math.Min(position.Quantity, remaining);
            allocations.Add(new FefoLotAllocation(position, allocatedQuantity));
            remaining -= allocatedQuantity;
        }

        return new FefoAllocationPlan(
            allocations,
            orderedPositions.FirstOrDefault(),
            remaining);
    }
}

public sealed record FefoLotAllocation(InventoryPosition Position, int Quantity);

public sealed record FefoAllocationPlan(
    IReadOnlyList<FefoLotAllocation> Allocations,
    InventoryPosition? SourceTemplate,
    int UnallocatedQuantity);
