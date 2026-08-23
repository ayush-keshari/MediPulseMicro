namespace InventoryService.DTOs;

// ── ExceptionEvent DTOs ───────────────────────────────────────────────────

public class ExceptionEventDto
{
    public int ExceptionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public int? ItemId { get; set; }
    public string? ItemName { get; set; }
    public int? FacilityId { get; set; }
    public string? LotId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
    public IEnumerable<RecallActionDto> Actions { get; set; } = [];
}

public class CreateExceptionRequest
{
    public string Type { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public int? ItemId { get; set; }
    public string? ItemName { get; set; }
    public int? FacilityId { get; set; }
    public string? LotId { get; set; }
    public string Severity { get; set; } = "Medium";
}

public class UpdateExceptionStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class DetectExceptionsResult
{
    public int StockoutCount { get; set; }
    public int ExpiryCount { get; set; }
    public int TotalCreated { get; set; }
}

// ── RecallAction DTOs ─────────────────────────────────────────────────────

public class RecallActionDto
{
    public int RecallActionId { get; set; }
    public int ExceptionId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateRecallActionRequest
{
    public int ExceptionId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class UpdateRecallActionRequest
{
    public string? ActionDescription { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
}
