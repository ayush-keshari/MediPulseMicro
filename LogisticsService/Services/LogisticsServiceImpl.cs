using LogisticsService.Data;
using LogisticsService.DTOs;
using LogisticsService.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsService.Services;

public class LogisticsServiceImpl : ILogisticsService
{
    private readonly LogisticsDbContext _db;

    public LogisticsServiceImpl(LogisticsDbContext db) => _db = db;

    // ── Transfer Orders ───────────────────────────────────────────────────

    public async Task<IEnumerable<TransferOrderDto>> GetAllTransferOrdersAsync()
        => await _db.TransferOrders
            .OrderByDescending(t => t.TransferOrderId)
            .Include(t => t.Items)
            .Select(t => ToTransferOrderDto(t))
            .ToListAsync();

    public async Task<IEnumerable<TransferOrderDto>> GetTransferOrdersByFacilityAsync(int facilityId)
        => await _db.TransferOrders
            .Where(t => t.FromFacilityId == facilityId || t.ToFacilityId == facilityId)
            .OrderByDescending(t => t.TransferOrderId)
            .Include(t => t.Items)
            .Select(t => ToTransferOrderDto(t))
            .ToListAsync();

    public async Task<TransferOrderDto?> GetTransferOrderByIdAsync(int id)
    {
        var t = await _db.TransferOrders
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TransferOrderId == id);
        return t == null ? null : ToTransferOrderDto(t);
    }

    public async Task<bool> CreateTransferOrderAsync(CreateTransferOrderRequest request)
    {
        if (request.FromFacilityId == request.ToFacilityId)
            throw new InvalidOperationException(
                "Source and destination facility cannot be the same.");

        // Validate each item has enough stock at the source facility
        foreach (var item in request.Items)
        {
            var available = await _db.InventoryPositions
                .Where(p => p.ItemId == item.ItemId && p.FacilityId == request.FromFacilityId && p.Quantity > 0)
                .SumAsync(p => (int?)p.Quantity) ?? 0;

            if (item.Quantity > available)
                throw new InvalidOperationException(
                    $"Insufficient stock for '{item.ItemName}' at the source facility. " +
                    $"Requested: {item.Quantity}, Available: {available}.");
        }

        var order = new TransferOrder
        {
            FromFacilityId = request.FromFacilityId,
            FromFacilityName = request.FromFacilityName,
            ToFacilityId = request.ToFacilityId,
            ToFacilityName = request.ToFacilityName,
            RequestedBy = request.RequestedBy,
            RequestedDate = DateTime.UtcNow,
            Status = "Draft",
            Items = request.Items.Select(i => new TransferOrderItem
            {
                ItemId = i.ItemId,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                ToStorageZoneId = i.ToStorageZoneId
            }).ToList()
        };

        _db.TransferOrders.Add(order);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateTransferOrderAsync(int id, UpdateTransferOrderRequest request)
    {
        var order = await _db.TransferOrders
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TransferOrderId == id);

        if (order == null) return false;

        if (order.Status != "Draft")
            throw new InvalidOperationException(
                $"Transfer order {id} cannot be edited in '{order.Status}' status. Only Draft orders can be modified.");

        _db.TransferOrderItems.RemoveRange(order.Items);

        order.Items = request.Items.Select(i => new TransferOrderItem
        {
            TransferOrderId = id,
            ItemId = i.ItemId,
            ItemName = i.ItemName,
            Quantity = i.Quantity,
            ToStorageZoneId = i.ToStorageZoneId
        }).ToList();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateTransferStatusAsync(int id, UpdateTransferStatusRequest request)
    {
        var order = await _db.TransferOrders
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TransferOrderId == id);
        if (order == null) return false;

        if (!IsValidStatusTransition(order.Status, request.Status))
            throw new InvalidOperationException(
                $"Cannot transition from '{order.Status}' to '{request.Status}'. " +
                "Allowed next statuses: " + string.Join(", ", GetAllowedNextStatuses(order.Status)));

        order.Status = request.Status;
        await _db.SaveChangesAsync();

        if (request.Status == "Completed")
        {
            foreach (var item in order.Items)
                await MoveStockAsync(item.ItemId, order.FromFacilityId, order.ToFacilityId, item.Quantity, order.TransferOrderId, item.ToStorageZoneId);
        }

        return true;
    }

    public async Task<bool> DeleteTransferOrderAsync(int id)
    {
        var order = await _db.TransferOrders
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.TransferOrderId == id);

        if (order == null) return false;

        if (order.Status != "Draft" && order.Status != "Cancelled")
            throw new InvalidOperationException(
                $"Transfer order {id} cannot be deleted in '{order.Status}' status. " +
                "Only Draft or Cancelled orders can be deleted.");

        _db.TransferOrderItems.RemoveRange(order.Items);
        _db.TransferOrders.Remove(order);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Consumption Records ───────────────────────────────────────────────

    public async Task<IEnumerable<ConsumptionRecordDto>> GetAllConsumptionAsync()
        => await _db.ConsumptionRecords
            .OrderByDescending(c => c.ConsumptionId)
            .Select(c => ToConsumptionDto(c))
            .ToListAsync();

    public async Task<IEnumerable<ConsumptionRecordDto>> GetConsumptionByFacilityAsync(int facilityId)
        => await _db.ConsumptionRecords
            .Where(c => c.FacilityId == facilityId)
            .OrderByDescending(c => c.ConsumptionId)
            .Select(c => ToConsumptionDto(c))
            .ToListAsync();

    public async Task<IEnumerable<ConsumptionRecordDto>> GetConsumptionByItemAsync(int itemId)
        => await _db.ConsumptionRecords
            .Where(c => c.ItemId == itemId)
            .OrderByDescending(c => c.ConsumptionId)
            .Select(c => ToConsumptionDto(c))
            .ToListAsync();

    public async Task<ConsumptionRecordDto?> GetConsumptionByIdAsync(int id)
    {
        var c = await _db.ConsumptionRecords.FindAsync(id);
        return c == null ? null : ToConsumptionDto(c);
    }

    public async Task<bool> CreateConsumptionAsync(CreateConsumptionRequest request)
    {
        var record = new ConsumptionRecord
        {
            FacilityId = request.FacilityId,
            WardId = request.WardId,
            ItemId = request.ItemId,
            ItemName = request.ItemName,
            QuantityConsumed = request.QuantityConsumed,
            ConsumedDate = request.ConsumedDate,
            ConsumedBy = request.ConsumedBy
        };

        _db.ConsumptionRecords.Add(record);
        await _db.SaveChangesAsync();

        await DeductStockAsync(request.ItemId, request.FacilityId, request.QuantityConsumed);

        return true;
    }

    public async Task<bool> UpdateConsumptionAsync(int id, UpdateConsumptionRequest request)
    {
        var record = await _db.ConsumptionRecords.FindAsync(id);
        if (record == null) return false;

        record.QuantityConsumed = request.QuantityConsumed;
        record.ConsumedDate = request.ConsumedDate;
        record.ConsumedBy = request.ConsumedBy;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteConsumptionAsync(int id)
    {
        var record = await _db.ConsumptionRecords.FindAsync(id);
        if (record == null) return false;

        _db.ConsumptionRecords.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Stock deduction (FEFO) ────────────────────────────────────────────
    // Deducts quantity from InventoryPositions directly in the shared DB.
    // Positions are consumed soonest-expiry-first (FEFO).
    private async Task DeductStockAsync(int itemId, int facilityId, int quantity)
    {
        var positions = await _db.InventoryPositions
            .Where(p => p.ItemId == itemId && p.FacilityId == facilityId && p.Quantity > 0)
            .OrderBy(p => p.ExpiryDate)
            .ToListAsync();

        var remaining = quantity;
        foreach (var pos in positions)
        {
            if (remaining <= 0) break;
            var take = Math.Min(pos.Quantity, remaining);
            pos.Quantity -= take;
            remaining -= take;
        }

        await _db.SaveChangesAsync();
    }

    // ── Stock movement on transfer completion (FEFO) ─────────────────────
    // Deducts from source facility (FEFO) and credits destination facility.
    // At destination: adds to an existing position for that item, or creates
    // a new one using the source lot's metadata as a template.
    private async Task MoveStockAsync(int itemId, int fromFacilityId, int toFacilityId, int quantity, int transferOrderId, int toStorageZoneId)
    {
        // ── 1. Deduct from source (FEFO) ──────────────────────────────────
        var sourcePositions = await _db.InventoryPositions
            .Where(p => p.ItemId == itemId && p.FacilityId == fromFacilityId && p.Quantity > 0)
            .OrderBy(p => p.ExpiryDate)
            .ToListAsync();

        // Keep a reference to the first source position deducted — used as
        // template for the destination if no position exists there yet.
        InventoryPosition? sourceTemplate = sourcePositions.FirstOrDefault();

        var remaining = quantity;
        foreach (var pos in sourcePositions)
        {
            if (remaining <= 0) break;
            var take = Math.Min(pos.Quantity, remaining);
            pos.Quantity -= take;
            remaining -= take;
        }

        // ── 2. Credit destination ─────────────────────────────────────────
        // Find an existing position for this item at the destination in the SAME zone the user chose
        var destPosition = await _db.InventoryPositions
            .Where(p => p.ItemId == itemId && p.FacilityId == toFacilityId && p.StorageZoneId == toStorageZoneId)
            .OrderBy(p => p.PositionId)
            .FirstOrDefaultAsync();

        if (destPosition != null)
        {
            // Item already exists in that zone at destination — just add to that position.
            destPosition.Quantity += quantity;
        }
        else
        {
            // No position exists for this item in the chosen zone — create one.
            _db.InventoryPositions.Add(new InventoryPosition
            {
                ItemId = itemId,
                LotId = $"XFER-{transferOrderId}",
                FacilityId = toFacilityId,
                StorageZoneId = toStorageZoneId,
                Quantity = quantity,
                SafetyStock = sourceTemplate?.SafetyStock ?? 0,
                ExpiryDate = sourceTemplate?.ExpiryDate ?? DateTime.UtcNow.AddYears(2),
            });
        }

        await _db.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static TransferOrderDto ToTransferOrderDto(TransferOrder t) => new()
    {
        TransferOrderId = t.TransferOrderId,
        FromFacilityId = t.FromFacilityId,
        FromFacilityName = t.FromFacilityName,
        ToFacilityId = t.ToFacilityId,
        ToFacilityName = t.ToFacilityName,
        RequestedBy = t.RequestedBy,
        RequestedDate = t.RequestedDate,
        Status = t.Status,
        Items = t.Items.Select(i => new TransferOrderItemDto
        {
            TransferOrderItemId = i.TransferOrderItemId,
            ItemId = i.ItemId,
            ItemName = i.ItemName,
            Quantity = i.Quantity,
            ToStorageZoneId = i.ToStorageZoneId
        }).ToList()
    };

    private static ConsumptionRecordDto ToConsumptionDto(ConsumptionRecord c) => new()
    {
        ConsumptionId = c.ConsumptionId,
        FacilityId = c.FacilityId,
        WardId = c.WardId,
        ItemId = c.ItemId,
        ItemName = c.ItemName,
        QuantityConsumed = c.QuantityConsumed,
        ConsumedDate = c.ConsumedDate,
        ConsumedBy = c.ConsumedBy
    };

    private static bool IsValidStatusTransition(string current, string next)
    {
        if (next == "Cancelled") return current != "Completed";
        return (current, next) switch
        {
            ("Draft", "Submitted") => true,
            ("Submitted", "Approved") => true,
            ("Submitted", "Draft") => true,
            ("Approved", "InTransit") => true,
            ("InTransit", "Completed") => true,
            _ => false
        };
    }

    private static IEnumerable<string> GetAllowedNextStatuses(string current) =>
        current switch
        {
            "Draft" => ["Submitted", "Cancelled"],
            "Submitted" => ["Approved", "Draft", "Cancelled"],
            "Approved" => ["InTransit", "Cancelled"],
            "InTransit" => ["Completed", "Cancelled"],
            _ => []
        };
}
