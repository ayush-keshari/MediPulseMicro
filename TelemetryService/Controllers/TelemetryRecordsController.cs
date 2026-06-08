using Microsoft.AspNetCore.Mvc;
using TelemetryService.DTOs;
using TelemetryService.Services;
using Shared.Constants;
using Shared.Filters;

namespace TelemetryService.Controllers;

[ApiController]
[Route("api/telemetry")]
[RoleAuthorize(Roles.Admin, Roles.ColdChainOperator, Roles.SupplyManager, Roles.ComplianceOfficer)]
public class TelemetryRecordsController : ControllerBase
{
    private readonly ITelemetryService _service;

    public TelemetryRecordsController(ITelemetryService service) => _service = service;

    // GET /api/telemetry
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllTelemetryAsync());

    // GET /api/telemetry/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _service.GetTelemetryByIdAsync(id);
        if (record == null) return NotFound(new { message = $"Telemetry record {id} not found." });
        return Ok(record);
    }

    // GET /api/telemetry/excursions
    [HttpGet("excursions")]
    public async Task<IActionResult> GetExcursions()
        => Ok(await _service.GetExcursionsAsync());

    // POST /api/telemetry
    // Excursion detection runs automatically on ingest.
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.ColdChainOperator)]
    public async Task<IActionResult> Create([FromBody] CreateTelemetryRecordRequest request)
    {
        try
        {
            await _service.CreateTelemetryAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // PUT /api/telemetry/{id}
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ColdChainOperator)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTelemetryRecordRequest request)
    {
        var updated = await _service.UpdateTelemetryAsync(id, request);
        if (!updated) return NotFound(new { message = $"Telemetry record {id} not found." });
        return NoContent();
    }

    // DELETE /api/telemetry/{id}
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteTelemetryAsync(id);
        if (!result) return NotFound(new { message = $"Telemetry record {id} not found." });
        return NoContent();
    }
}
