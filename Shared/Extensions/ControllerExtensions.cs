using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Shared.Filters;
using System.Net.Http;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for MVC controller configuration.
/// Centralizes controller setup to avoid duplication across services.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Adds controllers with global filters for validation, activity logging, and exception handling.
    /// Note: Exception handling is now done via middleware, so we only add ValidationFilter and ActivityLogFilter here.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddMediPulseControllers(this IServiceCollection services)
    {
        // Register HttpClientFactory for ActivityLogFilter
        services.AddHttpClient();

        services.AddControllers(options =>
        {
            // Add global filters
            options.Filters.Add<ValidationFilter>();      // Validates DTOs and returns 400 on failure
            options.Filters.Add<ActivityLogFilter>();     // Logs every request and response
            // Note: GlobalExceptionFilter is handled by UseMediPulseExceptionHandling middleware
        });
        return services;
    }
}