using LogisticsService.DTOs;

namespace LogisticsService.Services;

public interface ILogisticsService
{
    // ── Transfer Orders ───────────────────────────────────────────────────
    Task<IEnumerable<TransferOrderDto>> GetAllTransferOrdersAsync();
    Task<IEnumerable<TransferOrderDto>> GetTransferOrdersByFacilityAsync(int facilityId);
    Task<TransferOrderDto?> GetTransferOrderByIdAsync(int id);
    Task<bool> CreateTransferOrderAsync(CreateTransferOrderRequest request);
    Task<bool> UpdateTransferOrderAsync(int id, UpdateTransferOrderRequest request);
    Task<bool> UpdateTransferStatusAsync(int id, UpdateTransferStatusRequest request);

    // Only Draft or Cancelled orders can be deleted
    Task<bool> DeleteTransferOrderAsync(int id);

    // ── Consumption Records ───────────────────────────────────────────────
    Task<IEnumerable<ConsumptionRecordDto>> GetAllConsumptionAsync();
    Task<IEnumerable<ConsumptionRecordDto>> GetConsumptionByFacilityAsync(int facilityId);
    Task<IEnumerable<ConsumptionRecordDto>> GetConsumptionByItemAsync(int itemId);
    Task<ConsumptionRecordDto?> GetConsumptionByIdAsync(int id);
    Task<bool> CreateConsumptionAsync(CreateConsumptionRequest request);
    Task<bool> UpdateConsumptionAsync(int id, UpdateConsumptionRequest request);
    Task<bool> DeleteConsumptionAsync(int id);
}
