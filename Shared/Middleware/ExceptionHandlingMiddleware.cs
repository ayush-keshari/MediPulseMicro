using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Shared.Middleware
{
    /// <summary>
    /// Middleware to handle unhandled exceptions, enrich logs with correlation ID,
    /// and return a generic error response.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate or propagate correlation ID
            var correlationId = context.TraceIdentifier;
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Enrich logs with correlation ID
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    // Log the exception with correlation ID and request details
                    _logger.Error(ex,
                        "Unhandled exception occurred. CorrelationId: {CorrelationId}, Method: {Method}, Path: {Path}",
                        correlationId,
                        context.Request.Method,
                        context.Request.Path);

                    // Optionally set response status code and return a generic error
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var errorResponse = new
                    {
                        Error = "An unexpected error occurred. Please contact support.",
                        CorrelationId = correlationId
                    };

                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                }
            }
        }
    }

    /// <summary>
    /// Extension method to add the exception handling middleware to the pipeline.
    /// </summary>
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseMediPulseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}