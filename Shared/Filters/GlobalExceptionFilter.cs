using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

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
        _logger.LogError(context.Exception,
            "[EXCEPTION] {Method} {Path} => {Message}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.Exception.Message);

        context.Result = new ObjectResult(new
        {
            message = "An unexpected server error occurred.",
            detail = context.Exception.Message
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}
