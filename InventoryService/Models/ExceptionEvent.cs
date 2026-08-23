namespace InventoryService.Models;

// Represents a detected clinical supply problem that needs attention.
// Types: Stockout | ExpiryAlert | Excursion | Recall
// Severity: Low | Medium | High
// Status: Open | InProgress | Resolved | Dismissed
public class ExceptionEvent
{
    public int ExceptionId { get; set; }

    // What kind of problem
    public string Type { get; set; } = string.Empty;
    // Stockout    — item qty dropped below safety stock
    // ExpiryAlert — lot expiring within threshold days
    // Excursion   — temperature/humidity breach (reference to TelemetryId)
    // Recall      — supplier/regulator recall on a lot

    // Which entity triggered it (cross-service references stored as plain ints)
    public string ReferenceType { get; set; } = string.Empty;  // InventoryPosition | TransferOrder | Telemetry
    public int ReferenceId { get; set; }

    // Optional direct links for fast lookup
    public int? ItemId { get; set; }
    public string? ItemName { get; set; }
    public int? FacilityId { get; set; }
    public string? LotId { get; set; }

    public string Severity { get; set; } = "Medium";  // Low | Medium | High
    public string Status { get; set; } = "Open";    // Open | InProgress | Resolved | Dismissed

    public DateTime DetectedDate { get; set; } = DateTime.UtcNow;

    // Navigation — one exception can have many recall/corrective actions
    public ICollection<RecallAction> Actions { get; set; } = new List<RecallAction>();
}
