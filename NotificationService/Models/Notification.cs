namespace NotificationService.Models;

// A notification targeted at a specific user.
// Category (per spec): Exception | Expiry | Receipt | Replenishment
// IsRead implements the spec's "Status" field (read vs unread).
public class Notification
{
    public int NotificationId { get; set; }

    public string UserId { get; set; } = string.Empty;  // JWT sub / NameIdentifier

    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
