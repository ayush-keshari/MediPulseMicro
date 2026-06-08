using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogisticsService.Models;

// A line item inside a TransferOrder.
// ItemId is a cross-service reference to InventoryService — no EF FK.
// ItemName is denormalised for display.
public class TransferOrderItem
{
    [Key]
    public int TransferOrderItemId { get; set; }

    [Required]
    public int TransferOrderId { get; set; }

    // Cross-service reference to InventoryService Item
    [Required]
    public int ItemId { get; set; }

    [Required, MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    // The storage zone inside the destination facility where this item will be placed
    public int ToStorageZoneId { get; set; }

    // Navigation to parent TransferOrder
    [ForeignKey(nameof(TransferOrderId))]
    public TransferOrder? TransferOrder { get; set; }
}
