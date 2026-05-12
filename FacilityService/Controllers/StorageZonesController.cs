using FacilityService.DTOs;
using FacilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Filters;

namespace FacilityService.Controllers;

[ApiController]
[Route("api/storagezones")]
// Matches the monolith: Admin, SupplyManager, ColdChainOperator, ComplianceOfficer
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ColdChainOperator, Roles.ComplianceOfficer)]
public class StorageZonesController : ControllerBase
{
    private readonly IFacilityService _service;

    public StorageZonesController(IFacilityService service) => _service = service;

    // GET /api/storagezones
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var zones = await _service.GetAllZonesAsync();
        return Ok(zones);
    }

    // GET /api/storagezones/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var zone = await _service.GetZoneByIdAsync(id);
        if (zone == null) return NotFound(new { message = $"StorageZone {id} not found." });
        return Ok(zone);
    }

    // POST /api/storagezones
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStorageZoneRequest request)
    {
        var created = await _service.CreateZoneAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.ZoneId }, created);
    }

    // PUT /api/storagezones/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStorageZoneRequest request)
    {
        var updated = await _service.UpdateZoneAsync(id, request);
        if (updated == null) return NotFound(new { message = $"StorageZone {id} not found." });
        return Ok(updated);
    }

    // DELETE /api/storagezones/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteZoneAsync(id);
        if (!result) return NotFound(new { message = $"StorageZone {id} not found." });
        return NoContent();
    }
}
