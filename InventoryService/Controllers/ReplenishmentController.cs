using System.Security.Claims;
using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Constants;
using Shared.Filters;
using Shared.Helpers;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/replenishment")]
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager, Roles.ProcurementOfficer)]
public class ReplenishmentController : ControllerBase
{
    private readonly IReplenishmentService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _notifBaseUrl;

    private string CallerId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value ?? string.Empty;
    private string? BearerToken => HttpContext.Request.Headers["Authorization"].FirstOrDefault();

    public ReplenishmentController(
        IReplenishmentService service,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _notifBaseUrl = configuration["NotificationService:BaseUrl"];
    }

    // ── Forecasts ─────────────────────────────────────────────────────────

    // GET /api/replenishment/forecasts?facilityId=1&itemId=5
    [HttpGet("forecasts")]
    public async Task<IActionResult> GetForecasts(
        [FromQuery] int? facilityId,
        [FromQuery] int? itemId)
        => Ok(await _service.GetForecastsAsync(facilityId, itemId));

    // GET /api/replenishment/forecasts/{id}
    [HttpGet("forecasts/{id:int}")]
    public async Task<IActionResult> GetForecastById(int id)
    {
        var forecast = await _service.GetForecastByIdAsync(id);
        if (forecast == null) return NotFound(new { message = $"Forecast {id} not found." });
        return Ok(forecast);
    }

    // ── Plans ─────────────────────────────────────────────────────────────

    // GET /api/replenishment/plans?facilityId=1&status=Pending&priority=High
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(
        [FromQuery] int? facilityId,
        [FromQuery] string? status,
        [FromQuery] string? priority)
        => Ok(await _service.GetPlansAsync(facilityId, status, priority));

    // GET /api/replenishment/plans/{id}
    [HttpGet("plans/{id:int}")]
    public async Task<IActionResult> GetPlanById(int id)
    {
        var plan = await _service.GetPlanByIdAsync(id);
        if (plan == null) return NotFound(new { message = $"Plan {id} not found." });
        return Ok(plan);
    }

    // PATCH /api/replenishment/plans/{id}/status
    [HttpPatch("plans/{id:int}/status")]
    public async Task<IActionResult> UpdatePlanStatus(int id, [FromBody] UpdatePlanStatusRequest request)
    {
        var updated = await _service.UpdatePlanStatusAsync(id, request);
        if (!updated) return NotFound(new { message = $"Plan {id} not found." });
        return NoContent();
    }

    // DELETE /api/replenishment/plans/{id}
    [HttpDelete("plans/{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var result = await _service.DeletePlanAsync(id);
        if (!result) return NotFound(new { message = $"Plan {id} not found." });
        return NoContent();
    }

    // ── Generate ──────────────────────────────────────────────────────────

    // POST /api/replenishment/generate?facilityId=1
    // Scans inventory + last 30 days consumption, creates forecasts + plans.
    [HttpPost("generate")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Generate([FromQuery] int facilityId)
    {
        var result = await _service.GenerateAsync(facilityId);

        NotificationClient.Send(
            _httpClientFactory, _notifBaseUrl, BearerToken,
            userId: CallerId,
            category: "Replenishment",
            title: "Replenishment Plans Generated",
            message: $"{result.PlansCreated} plan(s) and {result.ForecastsCreated} forecast(s) generated for facility {result.FacilityId}");

        return Ok(result);
    }
}
