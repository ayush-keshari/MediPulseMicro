using Microsoft.AspNetCore.Mvc;
using TelemetryService.DTOs;
using TelemetryService.Services;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Filters;

namespace TelemetryService.Controllers;

[ApiController]
[Route("api/sensordevices")]
[RoleAuthorize(Roles.Admin, Roles.ColdChainOperator, Roles.SupplyManager)]
public class SensorDevicesController : ControllerBase
{
    private readonly ITelemetryService _service;

    public SensorDevicesController(ITelemetryService service) => _service = service;

    // GET /api/sensordevices
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllSensorsAsync());

    // GET /api/sensordevices/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sensor = await _service.GetSensorByIdAsync(id);
        if (sensor == null) return NotFound(new { message = $"Sensor {id} not found." });
        return Ok(sensor);
    }

    // GET /api/sensordevices/{id}/telemetry
    [HttpGet("{id:int}/telemetry")]
    public async Task<IActionResult> GetTelemetry(int id)
    {
        var sensor = await _service.GetSensorByIdAsync(id);
        if (sensor == null) return NotFound(new { message = $"Sensor {id} not found." });
        return Ok(await _service.GetTelemetryBySensorAsync(id));
    }

    // POST /api/sensordevices
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.ColdChainOperator)]
    public async Task<IActionResult> Create([FromBody] CreateSensorDeviceRequest request)
    {
        await _service.CreateSensorAsync(request);
        return NoContent();
    }

    // PUT /api/sensordevices/{id}
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ColdChainOperator)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSensorDeviceRequest request)
    {
        var updated = await _service.UpdateSensorAsync(id, request);
        if (!updated) return NotFound(new { message = $"Sensor {id} not found." });
        return NoContent();
    }

    // DELETE /api/sensordevices/{id}
    // Blocked when sensor has telemetry records.
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteSensorAsync(id);
            if (!result) return NotFound(new { message = $"Sensor {id} not found." });
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
