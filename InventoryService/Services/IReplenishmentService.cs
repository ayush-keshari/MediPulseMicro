using InventoryService.DTOs;

namespace InventoryService.Services;

public interface IReplenishmentService
{
    // Forecasts
    Task<IEnumerable<ForecastDto>> GetForecastsAsync(int? facilityId, int? itemId);
    Task<ForecastDto?>             GetForecastByIdAsync(int id);

    // Plans
    Task<IEnumerable<ReplenishmentPlanDto>> GetPlansAsync(int? facilityId, string? status, string? priority);
    Task<ReplenishmentPlanDto?>             GetPlanByIdAsync(int id);
    Task<bool>                              UpdatePlanStatusAsync(int id, UpdatePlanStatusRequest request);
    Task<bool>                              DeletePlanAsync(int id);

    // Generate: scan inventory and create forecasts + plans in one shot
    Task<GenerateReplenishmentResult>       GenerateAsync(int facilityId);
}
