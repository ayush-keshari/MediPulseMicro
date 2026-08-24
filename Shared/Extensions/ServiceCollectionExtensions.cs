using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for CORS configuration.
/// Centralizes CORS setup to avoid duplication across services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CORS policy allowing Angular application (port 4200) to call the Gateway/services.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the policy to.</param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddMediPulseCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
                policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }
}