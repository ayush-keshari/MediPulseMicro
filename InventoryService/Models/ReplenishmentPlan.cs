namespace InventoryService.Models;

// A suggested purchase order generated from current stock vs. safety stock + forecast.
// Priority: High (stockout) | Medium (below safety stock) | Low (approaching threshold)
// Status: Pending | Ordered | Fulfilled | Cancelled
public class ReplenishmentPlan
{
    public int PlanId { get; set; }
    public int ItemId { get; set; }
    public int FacilityId { get; set; }
    public int SuggestedOrderQty { get; set; }
    public string Priority { get; set; } = "Medium";  // High | Medium | Low
    public string Status { get; set; } = "Pending"; // Pending | Ordered | Fulfilled | Cancelled
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
}
