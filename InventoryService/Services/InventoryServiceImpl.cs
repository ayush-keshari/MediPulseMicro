using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public class InventoryServiceImpl : IInventoryService
{
    private readonly InventoryDbContext _context;

    public InventoryServiceImpl(InventoryDbContext context)
    {
        _context = context;
    }

    // ── ITEMS ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ItemResponse>> GetAllItemsAsync()
    {
        var items = await _context.Items
            .Include(i => i.Positions)
            .ToListAsync();

        return items.Select(MapItemToResponse);
    }

    public async Task<ItemResponse?> GetItemByIdAsync(int id)
    {
        var item = await _context.Items
            .Include(i => i.Positions)
            .FirstOrDefaultAsync(i => i.ItemId == id);

        return item is null ? null : MapItemToResponse(item);
    }

    public async Task<bool> CreateItemAsync(CreateItemRequest request)
    {
        // Pre-check duplicate ItemCode (case-insensitive) so the UI gets a clean 409
        // instead of a raw 500 from the SQL unique-index violation on IX_Items_ItemCode.
        // The DB constraint remains the source of truth — controller also catches
        // DbUpdateException as a safety net in case of a race condition.
        if (await _context.Items.AnyAsync(i => i.ItemCode.ToLower() == request.ItemCode.ToLower()))
            throw new InvalidOperationException($"An item with code '{request.ItemCode}' already exists.");

        var item = new Item
        {
            ItemCode           = request.ItemCode,
            Name               = request.Name,
            Category           = request.Category,
            Unit               = request.Unit,
            StorageRequirement = request.StorageRequirement,
            SafetyStock        = request.SafetyStock
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateItemAsync(int id, UpdateItemRequest request)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is null) return false;

        if (request.Name               is not null) item.Name               = request.Name;
        if (request.Category           is not null) item.Category           = request.Category;
        if (request.Unit               is not null) item.Unit               = request.Unit;
        if (request.StorageRequirement is not null) item.StorageRequirement = request.StorageRequirement;
        if (request.SafetyStock        is not null) item.SafetyStock        = request.SafetyStock.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is null) return false;

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── INVENTORY POSITIONS ───────────────────────────────────────────────

    public async Task<IEnumerable<PositionResponse>> GetAllPositionsAsync()
    {
        var positions = await _context.InventoryPositions
            .Include(p => p.Item)
            .OrderBy(p => p.ExpiryDate)   // FEFO
            .ToListAsync();

        return positions.Select(MapPositionToResponse);
    }

    public async Task<IEnumerable<PositionResponse>> GetPositionsByItemAsync(int itemId)
    {
        var positions = await _context.InventoryPositions
            .Include(p => p.Item)
            .Where(p => p.ItemId == itemId)
            .OrderBy(p => p.ExpiryDate)       // FEFO
            .ThenBy(p => p.PositionId)        // FIFO fallback
            .ToListAsync();

        return positions.Select(MapPositionToResponse);
    }

    public async Task<PositionResponse?> GetPositionByIdAsync(int id)
    {
        var position = await _context.InventoryPositions
            .Include(p => p.Item)
            .FirstOrDefaultAsync(p => p.PositionId == id);

        return position is null ? null : MapPositionToResponse(position);
    }

    public async Task<bool> CreatePositionAsync(CreatePositionRequest request)
    {
        var position = new InventoryPosition
        {
            ItemId        = request.ItemId,
            LotId         = request.LotId,
            ExpiryDate    = request.ExpiryDate,
            Quantity      = request.Quantity,
            FacilityId    = request.FacilityId,
            StorageZoneId = request.StorageZoneId,
            SafetyStock   = request.SafetyStock
        };

        _context.InventoryPositions.Add(position);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePositionAsync(int id, UpdatePositionRequest request)
    {
        var position = await _context.InventoryPositions.FindAsync(id);
        if (position is null) return false;

        if (request.Quantity      is not null) position.Quantity      = request.Quantity.Value;
        if (request.FacilityId    is not null) position.FacilityId    = request.FacilityId.Value;
        if (request.StorageZoneId is not null) position.StorageZoneId = request.StorageZoneId.Value;
        if (request.SafetyStock   is not null) position.SafetyStock   = request.SafetyStock.Value;
        if (request.ExpiryDate    is not null) position.ExpiryDate    = request.ExpiryDate.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePositionAsync(int id)
    {
        var position = await _context.InventoryPositions.FindAsync(id);
        if (position is null) return false;

        _context.InventoryPositions.Remove(position);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── PRIVATE MAPPERS ───────────────────────────────────────────────────

    private static ItemResponse MapItemToResponse(Item item) => new()
    {
        ItemId             = item.ItemId,
        ItemCode           = item.ItemCode,
        Name               = item.Name,
        Category           = item.Category,
        Unit               = item.Unit,
        StorageRequirement = item.StorageRequirement,
        SafetyStock        = item.SafetyStock,
        TotalStock         = item.Positions.Sum(p => p.Quantity)
    };

    private static PositionResponse MapPositionToResponse(InventoryPosition p) => new()
    {
        PositionId    = p.PositionId,
        ItemId        = p.ItemId,
        ItemName      = p.Item?.Name     ?? string.Empty,
        ItemCode      = p.Item?.ItemCode ?? string.Empty,
        LotId         = p.LotId,
        ExpiryDate    = p.ExpiryDate,
        Quantity      = p.Quantity,
        FacilityId    = p.FacilityId,
        StorageZoneId = p.StorageZoneId,
        SafetyStock   = p.SafetyStock
    };
}
