using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs;

public class CreatePositionRequest
{
    [Required]
    public int ItemId { get; set; }

    [Required, MaxLength(50)]
    public string LotId { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required]
    public int FacilityId { get; set; }

    [Required]
    public int StorageZoneId { get; set; }

    [Range(0, int.MaxValue)]
    public int SafetyStock { get; set; }
}

public class UpdatePositionRequest
{
    [Range(0, int.MaxValue)]
    public int? Quantity { get; set; }

    public int? FacilityId { get; set; }

    public int? StorageZoneId { get; set; }

    public int? SafetyStock { get; set; }

    public DateTime? ExpiryDate { get; set; }
}

// Lightweight summary: total available qty per item at a given facility
public class FacilityStockDto
{
    public int ItemId { get; set; }
    public int AvailableQty { get; set; }
}

public class PositionResponse
{
    public int PositionId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string LotId { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public int FacilityId { get; set; }
    public int StorageZoneId { get; set; }
    public int SafetyStock { get; set; }
    public bool IsExpired => ExpiryDate < DateTime.UtcNow;
    public bool IsExpiringSoon => ExpiryDate < DateTime.UtcNow.AddDays(90) && !IsExpired;
    public bool IsBelowSafetyStock => Quantity < SafetyStock;
}
