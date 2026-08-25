using FacilityService.DTOs;
using FacilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Filters;

namespace FacilityService.Controllers;

[ApiController]
[Route("api/storagezones")]
// GET endpoints include Nurse (needs zone list for stock-position dropdowns).
// Write operations are restricted at the method level — Nurse excluded.
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ColdChainOperator, Roles.ComplianceOfficer, Roles.Nurse)]
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

    // POST /api/storagezones  — Nurse cannot create zones
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ColdChainOperator, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Create([FromBody] CreateStorageZoneRequest request)
    {
        try
        {
            await _service.CreateZoneAsync(request);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/storagezones/{id}  — Nurse cannot edit zones
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ColdChainOperator, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStorageZoneRequest request)
    {
        try
        {
            var updated = await _service.UpdateZoneAsync(id, request);
            if (!updated) return NotFound(new { message = $"StorageZone {id} not found." });
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/storagezones/{id}  — Nurse cannot delete zones
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ColdChainOperator, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteZoneAsync(id);
        if (!result) return NotFound(new { message = $"StorageZone {id} not found." });
        return NoContent();
    }
}
