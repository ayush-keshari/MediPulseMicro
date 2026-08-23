namespace AuditService.Models;

// Stored in MedipulseAudit (separate database from MedipulseMain).
// Written by other services via POST /api/audit/log (currently from ActivityLogFilter).
// Read by Admin and ComplianceOfficer via GET /api/audit.
public class AuditLog
{
    public int AuditLogId { get; set; }

    // Who
    public string UserId { get; set; } = string.Empty;   // JWT sub / NameIdentifier
    public string? UserName { get; set; }
    public string? UserRole { get; set; }

    // What
    public string HttpMethod { get; set; } = string.Empty;   // GET, POST, PUT, PATCH, DELETE
    public string Endpoint { get; set; } = string.Empty;   // e.g. /api/suppliers/5
    public string? EntityType { get; set; }                   // e.g. Supplier, TransferOrder
    public string? EntityId { get; set; }                   // route param value
    public int StatusCode { get; set; }

    // Where
    public string? ServiceName { get; set; }                  // e.g. ProcurementService

    // When
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Extra
    public string? Details { get; set; }                   // optional JSON or note
}
