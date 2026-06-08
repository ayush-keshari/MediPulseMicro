using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public class ExceptionServiceImpl : IExceptionService
{
    private readonly InventoryDbContext _db;

    public ExceptionServiceImpl(InventoryDbContext db) => _db = db;

    // ── ExceptionEvents ───────────────────────────────────────────────────

    public async Task<IEnumerable<ExceptionEventDto>> GetAllAsync(
        string? type, string? status, string? severity)
    {
        var query = _db.ExceptionEvents.Include(e => e.Actions).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))     query = query.Where(e => e.Type     == type);
        if (!string.IsNullOrWhiteSpace(status))   query = query.Where(e => e.Status   == status);
        if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(e => e.Severity == severity);

        return await query
            .OrderByDescending(e => e.DetectedDate)
            .Select(e => ToEventDto(e))
            .ToListAsync();
    }

    public async Task<ExceptionEventDto?> GetByIdAsync(int id)
    {
        var e = await _db.ExceptionEvents
            .Include(e => e.Actions)
            .FirstOrDefaultAsync(e => e.ExceptionId == id);
        return e == null ? null : ToEventDto(e);
    }

    public async Task<bool> CreateAsync(CreateExceptionRequest request)
    {
        var ev = new ExceptionEvent
        {
            Type          = request.Type,
            ReferenceType = request.ReferenceType,
            ReferenceId   = request.ReferenceId,
            ItemId        = request.ItemId,
            ItemName      = request.ItemName,
            FacilityId    = request.FacilityId,
            LotId         = request.LotId,
            Severity      = request.Severity,
            Status        = "Open",
            DetectedDate  = DateTime.UtcNow
        };
        _db.ExceptionEvents.Add(ev);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateExceptionStatusRequest request)
    {
        var ev = await _db.ExceptionEvents.FindAsync(id);
        if (ev == null) return false;

        ev.Status = request.Status;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ev = await _db.ExceptionEvents.FindAsync(id);
        if (ev == null) return false;
        _db.ExceptionEvents.Remove(ev);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Auto-detect scan ──────────────────────────────────────────────────

    public async Task<DetectExceptionsResult> DetectAsync(int? facilityId, int expiryThresholdDays)
    {
        var result = new DetectExceptionsResult();
        var now    = DateTime.UtcNow;
        var expiryThreshold = now.AddDays(expiryThresholdDays);

        var posQuery = _db.InventoryPositions.Include(p => p.Item).AsQueryable();
        if (facilityId.HasValue)
            posQuery = posQuery.Where(p => p.FacilityId == facilityId.Value);

        var positions = await posQuery.ToListAsync();

        var openExceptions = await _db.ExceptionEvents
            .Where(e => e.Status == "Open" || e.Status == "InProgress")
            .ToListAsync();

        foreach (var pos in positions)
        {
            // ── Stockout check ────────────────────────────────────────────
            if (pos.Quantity < pos.SafetyStock)
            {
                bool alreadyOpen = openExceptions.Any(e =>
                    e.Type == "Stockout" &&
                    e.ReferenceType == "InventoryPosition" &&
                    e.ReferenceId == pos.PositionId);

                if (!alreadyOpen)
                {
                    _db.ExceptionEvents.Add(new ExceptionEvent
                    {
                        Type          = "Stockout",
                        ReferenceType = "InventoryPosition",
                        ReferenceId   = pos.PositionId,
                        ItemId        = pos.ItemId,
                        ItemName      = pos.Item?.Name,
                        FacilityId    = pos.FacilityId,
                        LotId         = pos.LotId,
                        Severity      = pos.Quantity == 0 ? "High" : "Medium",
                        Status        = "Open",
                        DetectedDate  = now
                    });
                    result.StockoutCount++;
                }
            }

            // ── Expiry check ──────────────────────────────────────────────
            if (pos.ExpiryDate <= expiryThreshold && pos.Quantity > 0)
            {
                bool alreadyOpen = openExceptions.Any(e =>
                    e.Type == "ExpiryAlert" &&
                    e.ReferenceType == "InventoryPosition" &&
                    e.ReferenceId == pos.PositionId);

                if (!alreadyOpen)
                {
                    var daysLeft = (pos.ExpiryDate - now).Days;
                    _db.ExceptionEvents.Add(new ExceptionEvent
                    {
                        Type          = "ExpiryAlert",
                        ReferenceType = "InventoryPosition",
                        ReferenceId   = pos.PositionId,
                        ItemId        = pos.ItemId,
                        ItemName      = pos.Item?.Name,
                        FacilityId    = pos.FacilityId,
                        LotId         = pos.LotId,
                        Severity      = daysLeft <= 7 ? "High" : daysLeft <= 14 ? "Medium" : "Low",
                        Status        = "Open",
                        DetectedDate  = now
                    });
                    result.ExpiryCount++;
                }
            }
        }

        await _db.SaveChangesAsync();
        result.TotalCreated = result.StockoutCount + result.ExpiryCount;
        return result;
    }

    // ── RecallActions ─────────────────────────────────────────────────────

    public async Task<IEnumerable<RecallActionDto>> GetActionsAsync(int exceptionId)
        => await _db.RecallActions
            .Where(a => a.ExceptionId == exceptionId)
            .OrderByDescending(a => a.RecallActionId)
            .Select(a => ToActionDto(a))
            .ToListAsync();

    public async Task<RecallActionDto?> GetActionByIdAsync(int id)
    {
        var a = await _db.RecallActions.FindAsync(id);
        return a == null ? null : ToActionDto(a);
    }

    public async Task<bool> CreateActionAsync(CreateRecallActionRequest request)
    {
        var ev = await _db.ExceptionEvents.FindAsync(request.ExceptionId);
        if (ev != null && ev.Status == "Open")
            ev.Status = "InProgress";

        var action = new RecallAction
        {
            ExceptionId       = request.ExceptionId,
            OwnerId           = request.OwnerId,
            ActionDescription = request.ActionDescription,
            DueDate           = request.DueDate,
            Status            = "Pending"
        };
        _db.RecallActions.Add(action);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateActionAsync(int id, UpdateRecallActionRequest request)
    {
        var action = await _db.RecallActions.FindAsync(id);
        if (action == null) return false;

        if (request.ActionDescription != null) action.ActionDescription = request.ActionDescription;
        if (request.DueDate           != null) action.DueDate           = request.DueDate.Value;
        if (request.Status            != null) action.Status            = request.Status;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteActionAsync(int id)
    {
        var action = await _db.RecallActions.FindAsync(id);
        if (action == null) return false;
        _db.RecallActions.Remove(action);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static ExceptionEventDto ToEventDto(ExceptionEvent e) => new()
    {
        ExceptionId   = e.ExceptionId,
        Type          = e.Type,
        ReferenceType = e.ReferenceType,
        ReferenceId   = e.ReferenceId,
        ItemId        = e.ItemId,
        ItemName      = e.ItemName,
        FacilityId    = e.FacilityId,
        LotId         = e.LotId,
        Severity      = e.Severity,
        Status        = e.Status,
        DetectedDate  = e.DetectedDate,
        Actions       = e.Actions.Select(a => ToActionDto(a)).ToList()
    };

    private static RecallActionDto ToActionDto(RecallAction a) => new()
    {
        RecallActionId    = a.RecallActionId,
        ExceptionId       = a.ExceptionId,
        OwnerId           = a.OwnerId,
        ActionDescription = a.ActionDescription,
        DueDate           = a.DueDate,
        Status            = a.Status
    };
}
