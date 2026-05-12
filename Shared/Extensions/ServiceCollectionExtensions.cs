using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Filters;

namespace Shared.Extensions;

// Extension methods on IServiceCollection.
// Each service's Program.cs calls these instead of writing the same 30+ lines every time.
// This is the standard .NET pattern for packaging reusable infrastructure setup.
public static class ServiceCollectionExtensions
{
    // ── JWT AUTHENTICATION ────────────────────────────────────────────────
    // Reads Jwt:Key / Jwt:Issuer / Jwt:Audience from appsettings.json.
    // Registers the JWT Bearer middleware that validates every incoming token.
    // Every service calls this ONE line:
    //   builder.Services.AddJwtAuthentication(builder.Configuration);
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing from configuration.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = config["Jwt:Issuer"],
                    ValidAudience            = config["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();
        return services;
    }

    // ── CONTROLLERS + GLOBAL FILTERS ─────────────────────────────────────
    // Registers controllers and attaches the three global filters to every action.
    // Every service calls this ONE line:
    //   builder.Services.AddMediPulseControllers();
    public static IServiceCollection AddMediPulseControllers(
        this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<GlobalExceptionFilter>(); // unhandled exceptions → JSON 500
            options.Filters.Add<ActivityLogFilter>();     // logs every request
            options.Filters.Add<ValidationFilter>();      // DTO annotations → JSON 400
        });

        // Disable [ApiController]'s built-in model validation response
        // so our ValidationFilter fully controls the 400 error format.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        return services;
    }

    // ── CORS ──────────────────────────────────────────────────────────────
    // Allows Angular (port 4200) to call the Gateway/services during development.
    // Every service calls this ONE line:
    //   builder.Services.AddMediPulseCors();
    public static IServiceCollection AddMediPulseCors(
        this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }

    // ── SWAGGER WITH JWT SUPPORT ──────────────────────────────────────────
    // Sets up Swagger UI with the "Authorize" padlock for JWT testing.
    // serviceName appears as the title in the Swagger UI (e.g. "Auth Service").
    // Every service calls this ONE line:
    //   builder.Services.AddMediPulseSwagger("Auth Service");
    public static IServiceCollection AddMediPulseSwagger(
        this IServiceCollection services, string serviceName)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = serviceName, Version = "v1" });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Paste your JWT token. Swagger adds 'Bearer ' prefix automatically."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
