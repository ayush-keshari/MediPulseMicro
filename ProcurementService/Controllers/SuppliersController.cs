using Microsoft.AspNetCore.Mvc;
using ProcurementService.DTOs;
using ProcurementService.Services;
using Shared.Constants;
using Shared.Filters;

namespace ProcurementService.Controllers;

[ApiController]
[Route("api/suppliers")]
[RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer, Roles.SupplyManager, Roles.ComplianceOfficer)]
public class SuppliersController : ControllerBase
{
    private readonly IProcurementService _service;

    public SuppliersController(IProcurementService service) => _service = service;

    // GET /api/suppliers
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllSuppliersAsync());

    // GET /api/suppliers/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await _service.GetSupplierByIdAsync(id);
        if (supplier == null) return NotFound(new { message = $"Supplier {id} not found." });
        return Ok(supplier);
    }

    // GET /api/suppliers/{id}/purchaseorders
    [HttpGet("{id:int}/purchaseorders")]
    public async Task<IActionResult> GetPurchaseOrders(int id)
    {
        var supplier = await _service.GetSupplierByIdAsync(id);
        if (supplier == null) return NotFound(new { message = $"Supplier {id} not found." });
        return Ok(await _service.GetPurchaseOrdersBySupplierAsync(id));
    }

    // POST /api/suppliers
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        try
        {
            await _service.CreateSupplierAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/suppliers/{id}
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierRequest request)
    {
        try
        {
            var updated = await _service.UpdateSupplierAsync(id, request);
            if (!updated) return NotFound(new { message = $"Supplier {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/suppliers/{id}
    // Blocked at DB level if supplier has active POs (Restrict FK)
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteSupplierAsync(id);
            if (!result) return NotFound(new { message = $"Supplier {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
