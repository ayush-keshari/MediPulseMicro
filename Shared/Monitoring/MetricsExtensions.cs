using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Shared.Monitoring;

public static class MetricsRegistry
{
    private static long _requests;
    private static long _errors;

    public static void RecordRequest(bool failed)
    {
        Interlocked.Increment(ref _requests);
        if (failed)
        {
            Interlocked.Increment(ref _errors);
        }
    }

    public static string Render()
    {
        return $"# HELP medipulse_http_requests_total Total HTTP requests handled.\n" +
               $"# TYPE medipulse_http_requests_total counter\n" +
               $"medipulse_http_requests_total {Interlocked.Read(ref _requests)}\n" +
               $"# HELP medipulse_http_errors_total Total HTTP requests returning a server error.\n" +
               $"# TYPE medipulse_http_errors_total counter\n" +
               $"medipulse_http_errors_total {Interlocked.Read(ref _errors)}\n";
    }
}

public sealed class MetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var failed = false;
        try
        {
            await next(context);
            failed = context.Response.StatusCode >= 500;
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            MetricsRegistry.RecordRequest(failed);
        }
    }
}

public static class MetricsExtensions
{
    public static IApplicationBuilder UseMediPulseMetrics(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MetricsMiddleware>();
    }

    public static IEndpointConventionBuilder MapMediPulseMetrics(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/metrics", () => Results.Text(MetricsRegistry.Render(), "text/plain"));
    }
}
