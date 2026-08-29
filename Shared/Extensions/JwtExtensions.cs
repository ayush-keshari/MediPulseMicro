using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Shared.Extensions;

/// <summary>
/// Extension methods for JWT authentication configuration.
/// Centralizes JWT setup to avoid duplication across services.
/// </summary>
public static class JwtExtensions
{
    /// <summary>
    /// Adds JWT authentication with configuration from appsettings.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The IConfiguration to read JWT settings from.</param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keyString = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key configuration is missing");
        var key = Encoding.ASCII.GetBytes(keyString);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"]
            };
        });

        return services;
    }
}
