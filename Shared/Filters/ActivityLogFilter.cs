using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Shared.Filters;

// Registered globally in every service's Program.cs.
// Logs EVERY request: who called what endpoint, and what the response was.
// This is the foundation for audit logging.
// Later: instead of just logging, this can publish an event to RabbitMQ
//        which AuditService consumes and writes to MedipulseAudit DB.
public class ActivityLogFilter : IActionFilter
{
    private readonly ILogger<ActivityLogFilter> _logger;

    public ActivityLogFilter(ILogger<ActivityLogFilter> logger)
    {
        _logger = logger;
    }

    // Runs BEFORE the controller action executes.
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user    = context.HttpContext.User;
        var userId  = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value
                   ?? "anonymous";
        var role    = user.FindFirst(ClaimTypes.Role)?.Value ?? "none";
        var method  = context.HttpContext.Request.Method;
        var path    = context.HttpContext.Request.Path;

        _logger.LogInformation(
            "[ACTIVITY] {Timestamp} | User: {UserId} | Role: {Role} | {Method} {Path}",
            DateTime.UtcNow, userId, role, method, path);
    }

    // Runs AFTER the controller action executes.
    public void OnActionExecuted(ActionExecutedContext context)
    {
        var statusCode = context.HttpContext.Response.StatusCode;
        var method     = context.HttpContext.Request.Method;
        var path       = context.HttpContext.Request.Path;

        _logger.LogInformation(
            "[ACTIVITY] Response: {StatusCode} | {Method} {Path}",
            statusCode, method, path);
    }
}
