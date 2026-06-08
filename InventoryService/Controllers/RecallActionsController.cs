using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Filters;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/recallactions")]
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager,
               Roles.DeviceManager, Roles.ComplianceOfficer)]
public class RecallActionsController : ControllerBase
{
    private readonly IExceptionService _service;

    public RecallActionsController(IExceptionService service) => _service = service;

    // GET /api/recallactions?exceptionId=5
    [HttpGet]
    public async Task<IActionResult> GetByException([FromQuery] int exceptionId)
        => Ok(await _service.GetActionsAsync(exceptionId));

    // GET /api/recallactions/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var action = await _service.GetActionByIdAsync(id);
        if (action == null) return NotFound(new { message = $"Recall action {id} not found." });
        return Ok(action);
    }

    // POST /api/recallactions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecallActionRequest request)
    {
        await _service.CreateActionAsync(request);
        return NoContent();
    }

    // PUT /api/recallactions/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRecallActionRequest request)
    {
        var updated = await _service.UpdateActionAsync(id, request);
        if (!updated) return NotFound(new { message = $"Recall action {id} not found." });
        return NoContent();
    }

    // DELETE /api/recallactions/{id}
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteActionAsync(id);
        if (!result) return NotFound(new { message = $"Recall action {id} not found." });
        return NoContent();
    }
}
