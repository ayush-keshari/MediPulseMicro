using System.Security.Claims;
using LogisticsService.DTOs;
using LogisticsService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Constants;
using Shared.Filters;
using Shared.Helpers;

namespace LogisticsService.Controllers;

[ApiController]
[Route("api/transferorders")]
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.ProcurementOfficer, Roles.DeviceManager)]
public class TransferOrdersController : ControllerBase
{
    private readonly ILogisticsService  _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string?            _notifBaseUrl;

    private string  CallerId    => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value ?? string.Empty;
    private string? BearerToken => HttpContext.Request.Headers["Authorization"].FirstOrDefault();

    public TransferOrdersController(
        ILogisticsService  service,
        IHttpClientFactory httpClientFactory,
        IConfiguration     configuration)
    {
        _service           = service;
        _httpClientFactory = httpClientFactory;
        _notifBaseUrl      = configuration["NotificationService:BaseUrl"];
    }

    // GET /api/transferorders
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllTransferOrdersAsync());

    // GET /api/transferorders/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _service.GetTransferOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = $"Transfer order {id} not found." });
        return Ok(order);
    }

    // GET /api/transferorders/facility/{facilityId}
    [HttpGet("facility/{facilityId:int}")]
    public async Task<IActionResult> GetByFacility(int facilityId)
        => Ok(await _service.GetTransferOrdersByFacilityAsync(facilityId));

    // POST /api/transferorders
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransferOrderRequest request)
    {
        try
        {
            var created = await _service.CreateTransferOrderAsync(request);

            NotificationClient.Send(
                _httpClientFactory, _notifBaseUrl, BearerToken,
                userId:   CallerId,
                category: "SystemAlert",
                title:    "Transfer Order Created",
                message:  $"Transfer order created: {request.FromFacilityName} → {request.ToFacilityName}");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /api/transferorders/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransferOrderRequest request)
    {
        try
        {
            var updated = await _service.UpdateTransferOrderAsync(id, request);
            if (!updated) return NotFound(new { message = $"Transfer order {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PATCH /api/transferorders/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTransferStatusRequest request)
    {
        try
        {
            var updated = await _service.UpdateTransferStatusAsync(id, request);
            if (!updated) return NotFound(new { message = $"Transfer order {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/transferorders/{id}
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.SupplyManager)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteTransferOrderAsync(id);
            if (!result) return NotFound(new { message = $"Transfer order {id} not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
