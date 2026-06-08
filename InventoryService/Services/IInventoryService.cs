using InventoryService.DTOs;

namespace InventoryService.Services;

public interface IInventoryService
{
    // Items
    Task<IEnumerable<ItemResponse>> GetAllItemsAsync();
    Task<ItemResponse?> GetItemByIdAsync(int id);
    Task<(ItemResponse? Item, string? Error)> CreateItemAsync(CreateItemRequest request);
    Task<bool> UpdateItemAsync(int id, UpdateItemRequest request);
    Task<bool> DeleteItemAsync(int id);

    // Inventory Positions
    Task<IEnumerable<PositionResponse>> GetAllPositionsAsync();
    Task<IEnumerable<PositionResponse>> GetPositionsByItemAsync(int itemId);
    Task<IEnumerable<int>> GetFacilityIdsByItemAsync(int itemId);
    Task<IEnumerable<int>> GetItemIdsByFacilityAsync(int facilityId);
    Task<IEnumerable<FacilityStockDto>> GetFacilityStockAsync(int facilityId);
    Task<PositionResponse?> GetPositionByIdAsync(int id);
    Task<bool> CreatePositionAsync(CreatePositionRequest request);
    Task<bool> UpdatePositionAsync(int id, UpdatePositionRequest request);
    Task<bool> DeletePositionAsync(int id);
}
