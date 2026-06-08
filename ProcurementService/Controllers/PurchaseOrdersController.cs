using Microsoft.AspNetCore.Mvc;
using ProcurementService.DTOs;
using ProcurementService.Services;
using Shared.Constants;
using Shared.Filters;

namespace ProcurementService.Controllers;

[ApiController]
[Route("api/purchaseorders")]
[RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer, Roles.SupplyManager, Roles.ComplianceOfficer)]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IProcurementService _service;

    public PurchaseOrdersController(IProcurementService service) => _service = service;

    // GET /api/purchaseorders
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllPurchaseOrdersAsync());

    // GET /api/purchaseorders/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _service.GetPurchaseOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = $"Purchase order {id} not found." });
        return Ok(order);
    }

    // GET /api/purchaseorders/{id}/receipts
    [HttpGet("{id:int}/receipts")]
    public async Task<IActionResult> GetReceipts(int id)
    {
        var order = await _service.GetPurchaseOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = $"Purchase order {id} not found." });
        return Ok(await _service.GetReceiptsByPoAsync(id));
    }

    // POST /api/purchaseorders — always creates in Draft status
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        try
        {
            await _service.CreatePurchaseOrderAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // PUT /api/purchaseorders/{id} — editable in Draft status only
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseOrderRequest request)
    {
        try
        {
            var updated = await _service.UpdatePurchaseOrderAsync(id, request);
            if (!updated) return NotFound(new { message = $"Purchase order {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PATCH /api/purchaseorders/{id}/status — lifecycle transitions
    // Draft→Submitted→Approved→Shipped→PartiallyReceived→FullyReceived | any→Cancelled
    [HttpPatch("{id:int}/status")]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer, Roles.SupplyManager)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePoStatusRequest request)
    {
        try
        {
            var updated = await _service.UpdatePoStatusAsync(id, request);
            if (!updated) return NotFound(new { message = $"Purchase order {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/purchaseorders/{id} — Draft or Cancelled only
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeletePurchaseOrderAsync(id);
            if (!result) return NotFound(new { message = $"Purchase order {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
