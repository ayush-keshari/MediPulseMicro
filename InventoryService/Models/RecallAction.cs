namespace InventoryService.Models;

// A corrective action assigned to a user to resolve an ExceptionEvent.
// Status: Pending | InProgress | Completed | Cancelled
public class RecallAction
{
    public int RecallActionId { get; set; }
    public int ExceptionId { get; set; }   // FK → ExceptionEvent

    // Assigned owner (cross-service user reference — stored as plain string)
    public string OwnerId { get; set; } = string.Empty;  // JWT UserId

    public string ActionDescription { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Pending";   // Pending | InProgress | Completed | Cancelled

    // Navigation
    public ExceptionEvent? Exception { get; set; }
}
