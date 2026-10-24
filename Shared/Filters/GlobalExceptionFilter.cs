using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Shared.Filters;

// Registered globally in every service's Program.cs via:
//   options.Filters.Add<GlobalExceptionFilter>()
//
// Catches ANY unhandled exception that escapes a controller action.
// Without this, ASP.NET Core returns an HTML error page or empty 500 —
// which Angular cannot parse. This ensures every error returns clean JSON.
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled || context.HttpContext.Response.HasStarted)
            return;

        var businessRule = context.Exception is BusinessRuleException;
        _logger.Log(businessRule ? LogLevel.Warning : LogLevel.Error, context.Exception,
            "[EXCEPTION] {Method} {Path} => {Message}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.Exception.Message);

        context.Result = new ObjectResult(new
        {
            message = businessRule ? context.Exception.Message : "An unexpected server error occurred.",
            errorCode = businessRule ? "BUSINESS_RULE_VIOLATION" : "INTERNAL_SERVER_ERROR",
            correlationId = context.HttpContext.TraceIdentifier
        })
        {
            StatusCode = businessRule ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}
