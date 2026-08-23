namespace NotificationService.DTOs;

// ── Read DTO ──────────────────────────────────────────────────────────────
public class NotificationDto
{
    public int NotificationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Create ────────────────────────────────────────────────────────────────
public class CreateNotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ── Query params for GET /api/notifications ───────────────────────────────
public class NotificationQueryParams
{
    public string? UserId { get; set; }
    public string? Category { get; set; }
    public bool? IsRead { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}

// ── Unread count ──────────────────────────────────────────────────────────
public class UnreadCountDto
{
    public int Count { get; set; }
}
