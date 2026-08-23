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
[Route("api/exceptions")]
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
               Roles.DeviceManager, Roles.ComplianceOfficer)]
public class ExceptionsController : ControllerBase
{
    private readonly IExceptionService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _notifBaseUrl;

    private string CallerId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value ?? string.Empty;
    private string? BearerToken => HttpContext.Request.Headers["Authorization"].FirstOrDefault();

    public ExceptionsController(
        IExceptionService service,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _notifBaseUrl = configuration["NotificationService:BaseUrl"];
    }

    // GET /api/exceptions?type=Stockout&status=Open&severity=High
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] string? severity)
        => Ok(await _service.GetAllAsync(type, status, severity));

    // GET /api/exceptions/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _service.GetByIdAsync(id);
        if (ev == null) return NotFound(new { message = $"Exception {id} not found." });
        return Ok(ev);
    }

    // POST /api/exceptions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExceptionRequest request)
    {
        var created = await _service.CreateAsync(request);

        NotificationClient.Send(
            _httpClientFactory, _notifBaseUrl, BearerToken,
            userId: CallerId,
            category: "Exception",
            title: "Exception Reported",
            message: $"Exception logged (Severity: {request.Severity})");

        return NoContent();
    }

    // PATCH /api/exceptions/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateExceptionStatusRequest request)
    {
        var updated = await _service.UpdateStatusAsync(id, request);
        if (!updated) return NotFound(new { message = $"Exception {id} not found." });
        return NoContent();
    }

    // DELETE /api/exceptions/{id}
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result) return NotFound(new { message = $"Exception {id} not found." });
        return NoContent();
    }

    // POST /api/exceptions/detect?facilityId=1&expiryThresholdDays=30
    // Scans all inventory positions and auto-creates Stockout + ExpiryAlert exceptions.
    [HttpPost("detect")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager)]
    public async Task<IActionResult> Detect(
        [FromQuery] int? facilityId,
        [FromQuery] int expiryThresholdDays = 30)
    {
        var result = await _service.DetectAsync(facilityId, expiryThresholdDays);

        if (result.TotalCreated > 0)
            NotificationClient.Send(
                _httpClientFactory, _notifBaseUrl, BearerToken,
                userId: CallerId,
                category: "Exception",
                title: "Exception Scan Complete",
                message: $"{result.TotalCreated} exception(s) detected: {result.StockoutCount} stockout(s), {result.ExpiryCount} expiry alert(s)");

        return Ok(result);
    }
}
