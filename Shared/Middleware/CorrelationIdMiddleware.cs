using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? "/"))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(string? supplied)
    {
        return !string.IsNullOrWhiteSpace(supplied) && supplied.Length <= 128
            ? supplied
            : Guid.NewGuid().ToString("N");
    }
}
