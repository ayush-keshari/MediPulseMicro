using System.ComponentModel.DataAnnotations;

namespace ProcurementService.DTOs;

// ── Supplier DTOs ─────────────────────────────────────────────────────────

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SupplierType { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateSupplierRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SupplierType is required.")]
    [RegularExpression("^(Manufacturer|Distributor|3PL)$",
        ErrorMessage = "SupplierType must be Manufacturer, Distributor, or 3PL.")]
    public string SupplierType { get; set; } = string.Empty;

    [RegularExpression("^(Active|Inactive|OnHold)$",
        ErrorMessage = "Status must be Active, Inactive, or OnHold.")]
    public string Status { get; set; } = "Active";
}

public class UpdateSupplierRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SupplierType is required.")]
    [RegularExpression("^(Manufacturer|Distributor|3PL)$",
        ErrorMessage = "SupplierType must be Manufacturer, Distributor, or 3PL.")]
    public string SupplierType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Active|Inactive|OnHold)$",
        ErrorMessage = "Status must be Active, Inactive, or OnHold.")]
    public string Status { get; set; } = string.Empty;
}

// ── PurchaseOrder DTOs ────────────────────────────────────────────────────

public class PurchaseOrderDto
{
    public int PoId { get; set; }
    public int SupplierId { get; set; }
    // Resolved via EF Include(po => po.Supplier) -- no HTTP call needed
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int ReceiptCount { get; set; }
}

public class CreatePurchaseOrderRequest
{
    [Required(ErrorMessage = "SupplierId is required.")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "OrderDate is required.")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpectedDeliveryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdatePurchaseOrderRequest
{
    [Required(ErrorMessage = "SupplierId is required.")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "OrderDate is required.")]
    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdatePoStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(
        "^(Draft|Submitted|Approved|Shipped|PartiallyReceived|FullyReceived|Cancelled)$",
        ErrorMessage = "Status must be one of: Draft, Submitted, Approved, Shipped, PartiallyReceived, FullyReceived, Cancelled.")]
    public string Status { get; set; } = string.Empty;
}

// ── Receipt DTOs ──────────────────────────────────────────────────────────

public class ReceiptDto
{
    public int ReceiptId { get; set; }
    public int PoId { get; set; }
    public string? SupplierLot { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string QualityStatus { get; set; } = string.Empty;
    public int QuantityReceived { get; set; }
    // Resolved via EF: Receipt -> PurchaseOrder -> Supplier
    public string SupplierName { get; set; } = string.Empty;
}

public class CreateReceiptRequest
{
    [Required(ErrorMessage = "PoId is required.")]
    public int PoId { get; set; }

    [MaxLength(100)]
    public string? SupplierLot { get; set; }

    [Required(ErrorMessage = "ReceivedDate is required.")]
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "ReceivedBy is required.")]
    [MaxLength(100)]
    public string ReceivedBy { get; set; } = string.Empty;

    [Required(ErrorMessage = "QualityStatus is required.")]
    [RegularExpression("^(Accepted|Rejected|OnHold)$",
        ErrorMessage = "QualityStatus must be Accepted, Rejected, or OnHold.")]
    public string QualityStatus { get; set; } = "Accepted";

    [Required(ErrorMessage = "QuantityReceived is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuantityReceived must be at least 1.")]
    public int QuantityReceived { get; set; }
}

public class UpdateReceiptRequest
{
    [MaxLength(100)]
    public string? SupplierLot { get; set; }

    [Required(ErrorMessage = "ReceivedDate is required.")]
    public DateTime ReceivedDate { get; set; }

    [Required(ErrorMessage = "ReceivedBy is required.")]
    [MaxLength(100)]
    public string ReceivedBy { get; set; } = string.Empty;

    [Required(ErrorMessage = "QualityStatus is required.")]
    [RegularExpression("^(Accepted|Rejected|OnHold)$",
        ErrorMessage = "QualityStatus must be Accepted, Rejected, or OnHold.")]
    public string QualityStatus { get; set; } = string.Empty;

    [Required(ErrorMessage = "QuantityReceived is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "QuantityReceived must be at least 1.")]
    public int QuantityReceived { get; set; }
}
