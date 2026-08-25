using LogisticsService.DTOs;
using LogisticsService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Exceptions;
using Shared.Filters;

namespace LogisticsService.Controllers;

[ApiController]
[Route("api/consumption")]
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.Nurse, Roles.PharmacyManager)]
public class ConsumptionController : ControllerBase
{
    private readonly ILogisticsService _service;

    public ConsumptionController(ILogisticsService service) => _service = service;

    // GET /api/consumption
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllConsumptionAsync());

    // GET /api/consumption/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _service.GetConsumptionByIdAsync(id);
        if (record == null) return NotFound(new { message = $"Consumption record {id} not found." });
        return Ok(record);
    }

    // GET /api/consumption/facility/{facilityId}
    [HttpGet("facility/{facilityId:int}")]
    public async Task<IActionResult> GetByFacility(int facilityId)
        => Ok(await _service.GetConsumptionByFacilityAsync(facilityId));

    // GET /api/consumption/item/{itemId}
    [HttpGet("item/{itemId:int}")]
    public async Task<IActionResult> GetByItem(int itemId)
        => Ok(await _service.GetConsumptionByItemAsync(itemId));

    // POST /api/consumption
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsumptionRequest request)
    {
        try
        {
            await _service.CreateConsumptionAsync(request);
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/consumption/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConsumptionRequest request)
    {
        try
        {
            var updated = await _service.UpdateConsumptionAsync(id, request);
            if (!updated) return NotFound(new { message = $"Consumption record {id} not found." });
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/consumption/{id}
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteConsumptionAsync(id);
            if (!result) return NotFound(new { message = $"Consumption record {id} not found." });
            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
