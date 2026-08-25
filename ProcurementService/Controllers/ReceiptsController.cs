using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProcurementService.DTOs;
using ProcurementService.Services;
using Shared.Constants;
using Shared.Filters;
using Shared.Helpers;
using Shared.Exceptions;

namespace ProcurementService.Controllers;

[ApiController]
[Route("api/receipts")]
[RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer, Roles.SupplyManager,
               Roles.PharmacyManager, Roles.ComplianceOfficer)]
public class ReceiptsController : ControllerBase
{
    private readonly IProcurementService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _notifBaseUrl;

    private string CallerId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("sub")?.Value ?? string.Empty;
    private string? BearerToken => HttpContext.Request.Headers["Authorization"].FirstOrDefault();

    public ReceiptsController(
        IProcurementService service,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _notifBaseUrl = configuration["NotificationService:BaseUrl"];
    }

    // GET /api/receipts
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllReceiptsAsync());

    // GET /api/receipts/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var receipt = await _service.GetReceiptByIdAsync(id);
        if (receipt == null) return NotFound(new { message = $"Receipt {id} not found." });
        return Ok(receipt);
    }

    // POST /api/receipts
    // PO must be Approved, Shipped, or PartiallyReceived.
    // Auto-advances PO to PartiallyReceived on first GRN.
    [HttpPost]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer, Roles.SupplyManager)]
    public async Task<IActionResult> Create([FromBody] CreateReceiptRequest request)
    {
        try
        {
            var created = await _service.CreateReceiptAsync(request);

            NotificationClient.Send(
                _httpClientFactory, _notifBaseUrl, BearerToken,
                userId: CallerId,
                category: "Receipt",
                title: "Goods Receipt Recorded",
                message: $"Goods receipt recorded for PO #{request.PoId} ({request.QuantityReceived} units, status: {request.QualityStatus})");

            return NoContent();
        }
        catch (BusinessRuleException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    // PUT /api/receipts/{id}
    // Used to correct lot numbers, dates, or change QualityStatus (e.g. Accepted → OnHold)
    [HttpPut("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ProcurementOfficer, Roles.SupplyManager, Roles.PharmacyManager)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReceiptRequest request)
    {
        var updated = await _service.UpdateReceiptAsync(id, request);
        if (!updated) return NotFound(new { message = $"Receipt {id} not found." });
        return NoContent();
    }

    // DELETE /api/receipts/{id} — Admin only; prefer quality-status correction over deletion
    [HttpDelete("{id:int}")]
    [RoleAuthorize(Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteReceiptAsync(id);
        if (!result) return NotFound(new { message = $"Receipt {id} not found." });
        return NoContent();
    }
}
