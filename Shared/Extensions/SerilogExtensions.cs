using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.ApplicationInsights;
using System;
using Microsoft.AspNetCore.Builder;

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
            .MinimumLevel.Information();

        // Add Application Insights if instrumentation key is configured
        var instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
        if (!string.IsNullOrEmpty(instrumentationKey))
        {
            loggerConfiguration.WriteTo.ApplicationInsights(instrumentationKey, null);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        // Use Serilog as the logging provider
        builder.Host.UseSerilog();

        return builder;
    }
}