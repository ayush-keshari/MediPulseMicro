using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for exception handling middleware.
/// Centralizes exception handling setup to avoid duplication across services.
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Adds exception handling middleware to the pipeline.
    /// </summary>
    /// <param name="app">The WebApplication to add the middleware to.</param>
    /// <returns>The same WebApplication instance for chaining.</returns>
    public static WebApplication UseMediPulseExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler("/error");

        app.Map("/error", (HttpContext context) =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionHandlerPathFeature =
                context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var logger = context.RequestServices.GetRequiredService<ILogger>();

            if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else if (exceptionHandlerPathFeature?.Error is UnauthorizedAccessException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }

            logger.LogError(exceptionHandlerPathFeature?.Error, "Unhandled exception");

            var errorResponse = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An unexpected error occurred",
                DetailedMessage = exceptionHandlerPathFeature?.Error?.Message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        });

        return app;
    }
}