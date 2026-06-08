using InventoryService.DTOs;

namespace InventoryService.Services;

public interface IExceptionService
{
    // ── ExceptionEvents ───────────────────────────────────────────────────
    Task<IEnumerable<ExceptionEventDto>> GetAllAsync(string? type, string? status, string? severity);
    Task<ExceptionEventDto?>             GetByIdAsync(int id);
    Task<bool>                           CreateAsync(CreateExceptionRequest request);
    Task<bool>                           UpdateStatusAsync(int id, UpdateExceptionStatusRequest request);
    Task<bool>                           DeleteAsync(int id);

    // Auto-scan inventory and create exceptions for stockouts / expiring lots
    Task<DetectExceptionsResult>         DetectAsync(int? facilityId, int expiryThresholdDays);

    // ── RecallActions ─────────────────────────────────────────────────────
    Task<IEnumerable<RecallActionDto>>   GetActionsAsync(int exceptionId);
    Task<RecallActionDto?>               GetActionByIdAsync(int id);
    Task<bool>                           CreateActionAsync(CreateRecallActionRequest request);
    Task<bool>                           UpdateActionAsync(int id, UpdateRecallActionRequest request);
    Task<bool>                           DeleteActionAsync(int id);
}
