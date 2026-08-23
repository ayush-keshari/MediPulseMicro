using System.ComponentModel.DataAnnotations;

namespace LogisticsService.DTOs;

// ── TransferOrderItem DTOs ────────────────────────────────────────────────

public class TransferOrderItemDto
{
    public int TransferOrderItemId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ToStorageZoneId { get; set; }
}

public class TransferOrderItemRequest
{
    [Required(ErrorMessage = "ItemId is required.")]
    public int ItemId { get; set; }

    [Required(ErrorMessage = "ItemName is required.")]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Destination storage zone is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid destination zone.")]
    public int ToStorageZoneId { get; set; }
}

// ── TransferOrder DTOs ────────────────────────────────────────────────────

public class TransferOrderDto
{
    public int TransferOrderId { get; set; }
    public int FromFacilityId { get; set; }
    public string FromFacilityName { get; set; } = string.Empty;
    public int ToFacilityId { get; set; }
    public string ToFacilityName { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<TransferOrderItemDto> Items { get; set; } = new();
}

public class CreateTransferOrderRequest
{
    [Required(ErrorMessage = "FromFacilityId is required.")]
    public int FromFacilityId { get; set; }

    [Required(ErrorMessage = "FromFacilityName is required.")]
    [MaxLength(100)]
    public string FromFacilityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ToFacilityId is required.")]
    public int ToFacilityId { get; set; }

    [Required(ErrorMessage = "ToFacilityName is required.")]
    [MaxLength(100)]
    public string ToFacilityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RequestedBy is required.")]
    [MaxLength(100)]
    public string RequestedBy { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one item is required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<TransferOrderItemRequest> Items { get; set; } = new();
}

public class UpdateTransferOrderRequest
{
    [Required(ErrorMessage = "At least one item is required.")]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<TransferOrderItemRequest> Items { get; set; } = new();
}

public class UpdateTransferStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(
        "^(Draft|Submitted|Approved|InTransit|Completed|Cancelled)$",
        ErrorMessage = "Status must be one of: Draft, Submitted, Approved, InTransit, Completed, Cancelled.")]
    public string Status { get; set; } = string.Empty;
}

// ── ConsumptionRecord DTOs ────────────────────────────────────────────────

public class ConsumptionRecordDto
{
    public int ConsumptionId { get; set; }
    public int FacilityId { get; set; }
    public int? WardId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int QuantityConsumed { get; set; }
    public DateTime ConsumedDate { get; set; }
    public string ConsumedBy { get; set; } = string.Empty;
}

public class CreateConsumptionRequest
{
    [Required(ErrorMessage = "FacilityId is required.")]
    public int FacilityId { get; set; }

    public int? WardId { get; set; }

    [Required(ErrorMessage = "ItemId is required.")]
    public int ItemId { get; set; }

    [Required(ErrorMessage = "ItemName is required.")]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "QuantityConsumed is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuantityConsumed must be at least 1.")]
    public int QuantityConsumed { get; set; }

    [Required(ErrorMessage = "ConsumedDate is required.")]
    public DateTime ConsumedDate { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "ConsumedBy is required.")]
    [MaxLength(100)]
    public string ConsumedBy { get; set; } = string.Empty;
}

public class UpdateConsumptionRequest
{
    [Required(ErrorMessage = "QuantityConsumed is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuantityConsumed must be at least 1.")]
    public int QuantityConsumed { get; set; }

    [Required(ErrorMessage = "ConsumedDate is required.")]
    public DateTime ConsumedDate { get; set; }

    [Required(ErrorMessage = "ConsumedBy is required.")]
    [MaxLength(100)]
    public string ConsumedBy { get; set; } = string.Empty;
}
