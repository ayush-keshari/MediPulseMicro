using FacilityService.DTOs;
using FacilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Filters;

namespace FacilityService.Controllers;

[ApiController]
[Route("api/facilities")]
// GET endpoints are open to all roles (Nurse needs facilities list for dropdowns).
// Write operations (POST/PUT/DELETE) are restricted at the method level — Nurse excluded.
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
               Roles.ProcurementOfficer, Roles.ColdChainOperator,
               Roles.DeviceManager, Roles.ComplianceOfficer, Roles.Nurse)]
public class FacilitiesController : ControllerBase
{
    private readonly IFacilityService _service;

    public FacilitiesController(IFacilityService service) => _service = service;

    // GET /api/facilities
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var facilities = await _service.GetAllFacilitiesAsync();
        return Ok(facilities);
    }

    // GET /api/facilities/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var facility = await _service.GetFacilityByIdAsync(id);
        if (facility == null) return NotFound(new { message = $"Facility {id} not found." });
        return Ok(facility);
    }

    // GET /api/facilities/{id}/zones
    [HttpGet("{id:int}/zones")]
    public async Task<IActionResult> GetZones(int id)
    {
        var zones = await _service.GetZonesByFacilityAsync(id);
        return Ok(zones);
    }

    // POST /api/facilities  — Nurse cannot create facilities
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
                   Roles.ProcurementOfficer, Roles.ColdChainOperator,
                   Roles.DeviceManager, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Create([FromBody] CreateFacilityRequest request)
    {
        try
        {
            await _service.CreateFacilityAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/facilities/{id}  — Nurse cannot edit facilities
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
                   Roles.ProcurementOfficer, Roles.ColdChainOperator,
                   Roles.DeviceManager, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFacilityRequest request)
    {
        try
        {
            var updated = await _service.UpdateFacilityAsync(id, request);
            if (!updated) return NotFound(new { message = $"Facility {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/facilities/{id}  — Nurse cannot delete facilities
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
                   Roles.ProcurementOfficer, Roles.ColdChainOperator,
                   Roles.DeviceManager, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteFacilityAsync(id);
        if (!result) return NotFound(new { message = $"Facility {id} not found." });
        return NoContent();
    }
}
