namespace LogisticsService.Models;

// Lightweight read/write mirror of InventoryService's InventoryPositions table.
// Same DB (MedipulseMain), so we can update rows directly without an HTTP call.
// ExcludeFromMigrations() in LogisticsDbContext ensures EF never tries to CREATE this table.
public class InventoryPosition
{
    public int PositionId { get; set; }
    public int ItemId { get; set; }
    public string LotId { get; set; } = string.Empty;
    public int FacilityId { get; set; }
    public int StorageZoneId { get; set; }
    public int Quantity { get; set; }
    public int SafetyStock { get; set; }
    public DateTime ExpiryDate { get; set; }
}
