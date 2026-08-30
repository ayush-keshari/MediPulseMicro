using System.Threading;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Monitoring;

public static class MetricsRegistry
{
    private static long _requests;
    private static long _errors;
    private static long _durationMilliseconds;
    private static long _durationSamples;
    private static string _serviceName = "MediPulse";

    public static void Configure(string serviceName) => _serviceName = serviceName;

    public static void RecordRequest(bool failed, long durationMilliseconds)
    {
        Interlocked.Increment(ref _requests);
        Interlocked.Add(ref _durationMilliseconds, durationMilliseconds);
        Interlocked.Increment(ref _durationSamples);
        if (failed)
        {
            Interlocked.Increment(ref _errors);
        }
    }

    public static string Render()
    {
        var serviceName = _serviceName.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"# HELP medipulse_http_requests_total Total HTTP requests handled.\n" +
               $"# TYPE medipulse_http_requests_total counter\n" +
               $"medipulse_http_requests_total{{service=\"{serviceName}\"}} {Interlocked.Read(ref _requests)}\n" +
               $"# HELP medipulse_http_errors_total Total HTTP requests returning a server error.\n" +
               $"# TYPE medipulse_http_errors_total counter\n" +
               $"medipulse_http_errors_total{{service=\"{serviceName}\"}} {Interlocked.Read(ref _errors)}\n" +
               $"# HELP medipulse_http_request_duration_milliseconds_sum Total request duration in milliseconds.\n" +
               $"# TYPE medipulse_http_request_duration_milliseconds_sum counter\n" +
               $"medipulse_http_request_duration_milliseconds_sum{{service=\"{serviceName}\"}} {Interlocked.Read(ref _durationMilliseconds)}\n" +
               $"# HELP medipulse_http_request_duration_milliseconds_count Number of measured requests.\n" +
               $"# TYPE medipulse_http_request_duration_milliseconds_count counter\n" +
               $"medipulse_http_request_duration_milliseconds_count{{service=\"{serviceName}\"}} {Interlocked.Read(ref _durationSamples)}\n";
    }
}

public sealed class MetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var failed = false;
        try
        {
            if (context.Request.Path == "/metrics")
            {
                context.Response.ContentType = "text/plain; version=0.0.4";
                await context.Response.WriteAsync(MetricsRegistry.Render());
            }
            else
            {
                await next(context);
            }
            failed = context.Response.StatusCode >= 500;
        }
        catch
        {
            failed = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            MetricsRegistry.RecordRequest(failed, stopwatch.ElapsedMilliseconds);
        }
    }
}

public static class MetricsExtensions
{
    public static IApplicationBuilder UseMediPulseMetrics(this IApplicationBuilder app)
    {
        var environment = app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
        MetricsRegistry.Configure(environment.ApplicationName);
        return app.UseMiddleware<MetricsMiddleware>();
    }

    public static IEndpointConventionBuilder MapMediPulseMetrics(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/metrics", () => Results.Text(MetricsRegistry.Render(), "text/plain"));
    }
}
