using FacilityService.DTOs;
using FacilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Filters;

namespace FacilityService.Controllers;

[ApiController]
[Route("api/facilities")]
// All clinical roles can manage facilities — matches the monolith's [Authorize] roles.
// Nurse is the only role excluded (view-only, no facility management needed).
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
               Roles.ProcurementOfficer, Roles.ColdChainOperator,
               Roles.DeviceManager, Roles.ComplianceOfficer)]
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

    // POST /api/facilities
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFacilityRequest request)
    {
        var created = await _service.CreateFacilityAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.FacilityId }, created);
    }

    // PUT /api/facilities/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFacilityRequest request)
    {
        var updated = await _service.UpdateFacilityAsync(id, request);
        if (updated == null) return NotFound(new { message = $"Facility {id} not found." });
        return Ok(updated);
    }

    // DELETE /api/facilities/{id}
    // Cascades: deletes all StorageZones of this facility first, then the facility.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteFacilityAsync(id);
        if (!result) return NotFound(new { message = $"Facility {id} not found." });
        return NoContent();
    }
}
