using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Filters;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/items")]
[JwtAuth]   // every endpoint in this controller requires a valid JWT token
public class ItemsController : ControllerBase
{
    private readonly IInventoryService _service;

    public ItemsController(IInventoryService service)
    {
        _service = service;
    }

    // GET api/items
    // Returns all items with their total stock count
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllItemsAsync();
        return Ok(items);
    }

    // GET api/items/5
    // Returns one item by ID, or 404 if not found
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetItemByIdAsync(id);
        if (item is null)
            return NotFound(new { message = $"Item with ID {id} not found." });

        return Ok(item);
    }

    // POST api/items
    // Creates a new item — only Admin or SupplyManager allowed
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request)
    {
        // ModelState validation is handled globally by ValidationFilter in Shared
        try
        {
            await _service.CreateItemAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // Service-level pre-check (duplicate ItemCode) — clean 409 with message
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            // Safety net: race condition where two requests pass the pre-check
            // and one trips the IX_Items_ItemCode unique index at SaveChanges time.
            return Conflict(new { message = $"An item with code '{request.ItemCode}' already exists." });
        }
    }

    // PUT api/items/5
    // Updates an existing item — only Admin or SupplyManager allowed
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateItemRequest request)
    {
        var updated = await _service.UpdateItemAsync(id, request);
        if (!updated)
            return NotFound(new { message = $"Item with ID {id} not found." });

        return NoContent();
    }

    // DELETE api/items/5
    // Deletes an item and all its positions (cascade) — Admin only
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteItemAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Item with ID {id} not found." });

        return NoContent();   // 204 — success, nothing to return
    }
}
