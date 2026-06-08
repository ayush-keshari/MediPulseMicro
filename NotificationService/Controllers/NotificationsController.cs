using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;
using Shared.Constants;
using Shared.Filters;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[RoleAuthorize(Roles.Admin, Roles.SupplyManager, Roles.PharmacyManager, Roles.DeviceManager,
               Roles.ProcurementOfficer, Roles.ColdChainOperator, Roles.Nurse,
               Roles.ComplianceOfficer)]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    // ── Helpers ───────────────────────────────────────────────────────────
    private string CallerId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value ?? string.Empty;
    private bool IsAdmin   => (User.FindFirst(ClaimTypes.Role)?.Value
                           ?? User.FindFirst("role")?.Value) == Roles.Admin;

    // ── GET /api/notifications ────────────────────────────────────────────
    // Admin sees all; every other role sees only their own.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] NotificationQueryParams query)
    {
        if (!IsAdmin) query.UserId = CallerId;
        return Ok(await _service.GetNotificationsAsync(query));
    }

    // ── GET /api/notifications/unread-count ───────────────────────────────
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
        => Ok(new UnreadCountDto { Count = await _service.GetUnreadCountAsync(CallerId) });

    // ── POST /api/notifications ───────────────────────────────────────────
    // Any authenticated caller (or other services posting internally) creates
    // notifications. The class-level [RoleAuthorize] already requires a valid
    // JWT, so no tighter restriction is needed here.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        await _service.CreateAsync(request);
        return NoContent();
    }

    // ── PATCH /api/notifications/{id}/read ────────────────────────────────
    // Mark a single notification as read. Non-admins can only mark their own.
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var result = await _service.MarkReadAsync(id, CallerId, IsAdmin);
        if (!result) return NotFound(new { message = $"Notification {id} not found or not accessible." });
        return NoContent();
    }

    // ── PATCH /api/notifications/read-all ─────────────────────────────────
    // Mark all of the caller's notifications as read.
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _service.MarkAllReadAsync(CallerId);
        return NoContent();
    }

    // ── DELETE /api/notifications/{id} ────────────────────────────────────
    // Admin can delete any; other roles can delete only their own.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id, CallerId, IsAdmin);
        if (!deleted) return NotFound(new { message = $"Notification {id} not found or not accessible." });
        return NoContent();
    }
}
