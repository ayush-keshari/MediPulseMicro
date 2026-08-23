namespace AuditService.DTOs;

// ── Read DTO ──────────────────────────────────────────────────────────────
public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public int StatusCode { get; set; }
    public string? ServiceName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

// ── Write DTO (used by ActivityLogFilter via HTTP POST) ───────────────────
public class CreateAuditLogRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserRole { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public int StatusCode { get; set; }
    public string? ServiceName { get; set; }
    public string? Details { get; set; }
}

// ── Query parameters for GET /api/audit ──────────────────────────────────
public class AuditQueryParams
{
    public string? UserId { get; set; }
    public string? UserRole { get; set; }
    public string? HttpMethod { get; set; }
    public string? EntityType { get; set; }
    public string? ServiceName { get; set; }
    public int? StatusCode { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ── Paginated response wrapper ────────────────────────────────────────────
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Pages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}
