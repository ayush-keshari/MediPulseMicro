using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Filters;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/inventory")]
[JwtAuth]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service)
    {
        _service = service;
    }

    // GET api/inventory
    // Returns all positions across all items, ordered by expiry date (FEFO)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var positions = await _service.GetAllPositionsAsync();
        return Ok(positions);
    }

    // GET api/inventory/5
    // Returns a single position by ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var position = await _service.GetPositionByIdAsync(id);
        if (position is null)
            return NotFound(new { message = $"Inventory position with ID {id} not found." });

        return Ok(position);
    }

    // GET api/inventory/item/3
    // Returns all stock positions for a specific item, FEFO + FIFO ordered
    // This is the key endpoint — tells you which lot to consume next
    [HttpGet("item/{itemId:int}")]
    public async Task<IActionResult> GetByItem(int itemId)
    {
        var positions = await _service.GetPositionsByItemAsync(itemId);
        return Ok(positions);
    }

    // GET api/inventory/item/3/facilities
    // Returns distinct facility IDs that have stock (quantity > 0) for the given item
    [HttpGet("item/{itemId:int}/facilities")]
    public async Task<IActionResult> GetFacilitiesByItem(int itemId)
    {
        var facilityIds = await _service.GetFacilityIdsByItemAsync(itemId);
        return Ok(facilityIds);
    }

    // GET api/inventory/facility/2/items
    // Returns distinct item IDs that have stock (quantity > 0) at the given facility
    [HttpGet("facility/{facilityId:int}/items")]
    public async Task<IActionResult> GetItemsByFacility(int facilityId)
    {
        var itemIds = await _service.GetItemIdsByFacilityAsync(facilityId);
        return Ok(itemIds);
    }

    // GET api/inventory/facility/2/stock
    // Returns total available qty per item at the given facility (for transfer qty validation)
    [HttpGet("facility/{facilityId:int}/stock")]
    public async Task<IActionResult> GetFacilityStock(int facilityId)
    {
        var stock = await _service.GetFacilityStockAsync(facilityId);
        return Ok(stock);
    }

    // POST api/inventory
    // Records a new stock batch arriving (e.g. a shipment received)
    // Only ProcurementOfficer, SupplyManager or Admin can receive stock
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ProcurementOfficer)]
    public async Task<IActionResult> Create([FromBody] CreatePositionRequest request)
    {
        await _service.CreatePositionAsync(request);
        return NoContent();
    }

    // PUT api/inventory/5
    // Adjusts a position (e.g. after a stock count or consumption)
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.Nurse)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePositionRequest request)
    {
        var updated = await _service.UpdatePositionAsync(id, request);
        if (!updated)
            return NotFound(new { message = $"Inventory position with ID {id} not found." });

        return NoContent();
    }

    // DELETE api/inventory/5
    // Removes a position (e.g. expired lot disposal) — Admin or SupplyManager only
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeletePositionAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Inventory position with ID {id} not found." });

        return NoContent();
    }
}
