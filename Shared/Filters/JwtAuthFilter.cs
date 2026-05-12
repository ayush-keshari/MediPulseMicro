using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Filters;

// ── USAGE ──────────────────────────────────────────────────────────────────
// Apply [JwtAuth] on any controller or action that requires a logged-in user.
// Replaces the built-in [Authorize] attribute with a consistent JSON response.
//
//   [JwtAuth]                      ← any authenticated user
//   public IActionResult GetMe() { ... }
//
// How it works:
//   UseAuthentication() middleware already validated the JWT and populated
//   HttpContext.User before this filter runs. We just check the result here.
// ──────────────────────────────────────────────────────────────────────────

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JwtAuthAttribute : TypeFilterAttribute
{
    // TypeFilterAttribute tells ASP.NET Core to create JwtAuthFilter via DI
    // each time this attribute is applied, instead of using a shared instance.
    public JwtAuthAttribute() : base(typeof(JwtAuthFilter)) { }
}

public class JwtAuthFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // IsAuthenticated is true only when UseAuthentication() successfully
        // decoded a valid, non-expired JWT from the Authorization header.
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Authentication required. Please provide a valid JWT token."
            });
        }
    }
}
