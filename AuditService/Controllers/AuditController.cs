using AuditService.DTOs;
using AuditService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Filters;

namespace AuditService.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditController(IAuditLogService service) => _service = service;

    // POST /api/audit/log
    // Called by other services (via ActivityLogFilter) to record activity.
    // No [RoleAuthorize] — services authenticate via the same JWT, or this
    // endpoint can be called internally. For now it accepts any valid JWT.
    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] CreateAuditLogRequest request)
    {
        await _service.CreateAsync(request);
        return NoContent();
    }

    // GET /api/audit
    // Admin and ComplianceOfficer can query the full log with optional filters.
    [HttpGet]
    [RoleAuthorize(Roles.Admin, Roles.ComplianceOfficer)]
    public async Task<IActionResult> Query([FromQuery] AuditQueryParams query)
        => Ok(await _service.QueryAsync(query));

    // GET /api/audit/{id}
    [HttpGet("{id:int}")]
    [RoleAuthorize(Roles.Admin, Roles.ComplianceOfficer)]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _service.GetByIdAsync(id);
        if (log == null) return NotFound(new { message = $"Audit log {id} not found." });
        return Ok(log);
    }
}
