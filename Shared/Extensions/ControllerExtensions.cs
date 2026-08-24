using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for MVC controller configuration.
/// Centralizes controller setup to avoid duplication across services.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Adds controllers with default configuration.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddMediPulseControllers(this IServiceCollection services)
    {
        services.AddControllers();
        return services;
    }
}