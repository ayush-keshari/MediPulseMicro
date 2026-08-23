namespace InventoryService.DTOs;

// ── Forecast DTOs ─────────────────────────────────────────────────────────

public class ForecastDto
{
    public int ForecastId { get; set; }
    public int ItemId { get; set; }
    public int FacilityId { get; set; }
    public string Period { get; set; } = string.Empty;
    public int ForecastQuantity { get; set; }
    public DateTime GeneratedDate { get; set; }
}

// ── ReplenishmentPlan DTOs ────────────────────────────────────────────────

public class ReplenishmentPlanDto
{
    public int PlanId { get; set; }
    public int ItemId { get; set; }
    public int FacilityId { get; set; }
    public int SuggestedOrderQty { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
}

public class UpdatePlanStatusRequest
{
    public string Status { get; set; } = string.Empty;  // Pending | Ordered | Fulfilled | Cancelled
}

public class GenerateReplenishmentResult
{
    public int PlansCreated { get; set; }
    public int ForecastsCreated { get; set; }
    public int FacilityId { get; set; }
}
