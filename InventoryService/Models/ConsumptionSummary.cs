using Microsoft.EntityFrameworkCore;

namespace InventoryService.Models;

// Read-only projection of the ConsumptionRecord table owned by LogisticsService.
// Both services share MedipulseMain, so InventoryService can read this table
// directly for replenishment/forecast calculations without an HTTP call.
// [Keyless] — EF will not try to create or migrate this table.
[Keyless]
public class ConsumptionSummary
{
    public int ItemId { get; set; }
    public int FacilityId { get; set; }
    public int QuantityConsumed { get; set; }
    public DateTime ConsumedDate { get; set; }
}
