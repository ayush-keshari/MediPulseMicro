using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.Models;

public class InventoryPosition
{
    [Key]
    public int PositionId { get; set; }

    // Foreign key — which item does this batch belong to?
    public int ItemId { get; set; }

    [Required, MaxLength(50)]
    public string LotId { get; set; } = string.Empty;      // batch/lot number on the packaging

    public DateTime ExpiryDate { get; set; }               // FEFO: soonest expiry is consumed first

    public int Quantity { get; set; }                      // current units in stock for this lot

    public int FacilityId { get; set; }                    // which facility holds this lot

    public int StorageZoneId { get; set; }                 // which zone inside that facility

    public int SafetyStock { get; set; }                   // reorder threshold for this item at this facility

    // UTC timestamp for freshness checks and operational backfill tracking.
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;

    // Navigation property — lets us access Item.Name, Item.Unit etc. from a position
    [ForeignKey(nameof(ItemId))]
    public Item? Item { get; set; }
}
