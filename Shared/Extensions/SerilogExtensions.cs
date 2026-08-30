using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.ApplicationInsights;
using System;
using Microsoft.AspNetCore.Builder;
using Serilog.Events;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for Serilog logging configuration.
/// Centralizes logging setup to avoid duplication across services.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Adds Serilog logging with JSON console output and optional Application Insights.
    /// Each service's Program.cs should call this BEFORE creating the WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder.</param>
    /// <returns>The same WebApplicationBuilder instance for chaining.</returns>
    public static WebApplicationBuilder AddMediPulseSerilog(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration["ServiceName"] ?? "UnknownService";
        var environment = builder.Environment.EnvironmentName;

        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console(new JsonFormatter())
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning);

        // Send the same structured events to Application Insights when configured.
        var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        var instrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"]
            ?? Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            loggerConfiguration.WriteTo.ApplicationInsights(connectionString, TelemetryConverter.Traces);
        }
        else if (!string.IsNullOrWhiteSpace(instrumentationKey))
        {
            loggerConfiguration.WriteTo.ApplicationInsights(instrumentationKey, TelemetryConverter.Traces);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        // Use Serilog as the logging provider
        builder.Host.UseSerilog();

        return builder;
    }
}
