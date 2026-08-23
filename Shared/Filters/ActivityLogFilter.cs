using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Filters;

// Registered globally in every service's Program.cs.
// Captures EVERY request: who called what endpoint, and what the response was.
//
// On each completed request it does two things:
//   1. Logs to console via ILogger (always).
//   2. Fire-and-forget POST to AuditService /api/audit/log (if "AuditService:BaseUrl"
//      is set in appsettings.json). Failures are swallowed so a downed AuditService
//      never blocks operational traffic.
//
// To enable audit writing, add to appsettings.json in each service:
//   "AuditService": { "BaseUrl": "http://localhost:5008" }
//
// To disable (e.g. in AuditService itself to avoid infinite loops), simply omit
// the "AuditService:BaseUrl" key.
public class ActivityLogFilter : IActionFilter
{
    private readonly ILogger<ActivityLogFilter> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _auditServiceUrl;
    private readonly string? _serviceName;

    // Context captured in OnActionExecuting, consumed in OnActionExecuted.
    private string _userId = "anonymous";
    private string _role = "none";
    private string _method = string.Empty;
    private string _path = string.Empty;
    private string? _entityType;
    private string? _entityId;

    public ActivityLogFilter(
        ILogger<ActivityLogFilter> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _auditServiceUrl = configuration["AuditService:BaseUrl"]?.TrimEnd('/');
        _serviceName = configuration["ServiceName"];
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // Try multiple claim type variants to handle differences across .NET versions
        // and whether JsonWebTokenHandler or JwtSecurityTokenHandler is used.
        // Final fallback: decode the JWT token directly from the Authorization header.
        _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value   // mapped "sub" (legacy handler)
               ?? user.FindFirst("sub")?.Value                        // unmapped "sub" (new handler)
               ?? ReadJwtClaim(context.HttpContext, "sub")             // direct JWT decode fallback
               ?? "anonymous";

        _role = user.FindFirst(ClaimTypes.Role)?.Value                // mapped role (legacy handler)
             ?? user.FindFirst("role")?.Value                          // unmapped "role" (new handler)
             ?? ReadJwtClaim(context.HttpContext, "role")               // direct JWT decode fallback
             ?? "none";

        _method = context.HttpContext.Request.Method;
        _path = context.HttpContext.Request.Path;

        // Try to extract entity type from path segments: /api/{entityType}/{id}
        var segments = _path.Trim('/').Split('/');
        if (segments.Length >= 2) _entityType = Capitalize(segments[1]);
        if (segments.Length >= 3 && int.TryParse(segments[2], out _)) _entityId = segments[2];

        _logger.LogInformation(
            "[ACTIVITY] {Timestamp} | User: {UserId} | Role: {Role} | {Method} {Path}",
            DateTime.UtcNow, _userId, _role, _method, _path);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var statusCode = context.HttpContext.Response.StatusCode;

        _logger.LogInformation(
            "[ACTIVITY] Response: {StatusCode} | {Method} {Path}",
            statusCode, _method, _path);

        // Only forward to AuditService when a URL is configured.
        if (string.IsNullOrEmpty(_auditServiceUrl)) return;

        // Only log mutating operations (skip plain GETs to reduce noise).
        if (_method == "GET") return;

        // For unauthenticated endpoints like login, the userId is "anonymous" on entry because
        // there is no JWT in the incoming request — the user is exchanging credentials FOR a token.
        // If the response contains a "token" field (login succeeded), decode it to capture who logged in.
        // We also grab "name" directly from the response body (AuthResponse has a Name property).
        string? responseJwt = null;
        string? responseUserName = null;
        if (_userId == "anonymous" && context.Result is ObjectResult { Value: not null } objResult)
            (responseJwt, responseUserName) = ExtractFromResponse(objResult.Value);

        if (!string.IsNullOrEmpty(responseJwt))
        {
            _userId = DecodeJwtClaim(responseJwt, "sub") ?? _userId;
            _role = DecodeJwtClaim(responseJwt, "role") ?? _role;
        }

        var user = context.HttpContext.User;
        // "unique_name" is what JwtSecurityTokenHandler writes for ClaimTypes.Name outbound.
        var userName = user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst("unique_name")?.Value
                    ?? user.FindFirst("name")?.Value
                    ?? ReadJwtClaim(context.HttpContext, "unique_name")
                    ?? ReadJwtClaim(context.HttpContext, "name")
                    ?? responseUserName                                           // direct from login response body
                    ?? (responseJwt != null ? DecodeJwtClaim(responseJwt, "unique_name") : null);

        var payload = new
        {
            userId = _userId,
            userName,
            userRole = _role,
            httpMethod = _method,
            endpoint = _path,
            entityType = _entityType,
            entityId = _entityId,
            statusCode,
            serviceName = _serviceName
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Fire-and-forget: copy the bearer token from the current request so
        // AuditService can verify it came from an authenticated caller.
        // For login (no inbound token), forward the newly-issued token from the response instead.
        var authToken = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()
                     ?? (responseJwt != null ? $"Bearer {responseJwt}" : null);

        _ = Task.Run(async () =>
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AuditService");
                if (!string.IsNullOrEmpty(authToken))
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authToken);

                await client.PostAsync($"{_auditServiceUrl}/api/audit/log", content);
            }
            catch
            {
                // Swallow — a downed AuditService must never affect operational requests.
            }
        });
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    // Reads a claim from the JWT in the incoming Authorization: Bearer header.
    private static string? ReadJwtClaim(HttpContext context, string claimType)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return DecodeJwtClaim(authHeader["Bearer ".Length..].Trim(), claimType);
    }

    // Serialises the response body once and extracts both the JWT token and the display
    // name in a single pass — used for unauthenticated endpoints like login where the
    // user identity is only known from the response, not the incoming request.
    // Uses the RUNTIME type (not the declared 'object' type) so all properties are included.
    private static (string? token, string? name) ExtractFromResponse(object responseValue)
    {
        string? token = null, name = null;
        try
        {
            // Passing responseValue.GetType() ensures System.Text.Json uses the runtime
            // type (e.g. AuthResponse) instead of 'object', which would produce "{}".
            var json = JsonSerializer.Serialize(responseValue, responseValue.GetType());
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.String) continue;
                if (prop.Name.Equals("token", StringComparison.OrdinalIgnoreCase))
                    token = prop.Value.GetString();
                else if (prop.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                    name = string.IsNullOrWhiteSpace(prop.Value.GetString()) ? null : prop.Value.GetString();
            }
        }
        catch { }
        return (token, name);
    }

    // Decodes a raw JWT string (without validation) and returns the value of the
    // requested claim. Uses case-insensitive matching so differences in how various
    // versions of JwtSecurityTokenHandler capitalise claim names don't matter.
    private static string? DecodeJwtClaim(string token, string claimType)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            // Decode base64url payload (part[1]) — pad to a multiple of 4 first
            var b64 = parts[1];
            b64 = (b64.Length % 4) switch
            {
                2 => b64 + "==",
                3 => b64 + "=",
                _ => b64
            };
            var jsonBytes = Convert.FromBase64String(b64.Replace('-', '+').Replace('_', '/'));
            var json = Encoding.UTF8.GetString(jsonBytes);

            using var doc = JsonDocument.Parse(json);

            // Case-insensitive search — JWT claim name casing can vary across
            // versions and libraries (e.g. "role" vs "Role" vs the full URI form).
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!prop.Name.Equals(claimType, StringComparison.OrdinalIgnoreCase)) continue;
                return prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    _ => null
                };
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
