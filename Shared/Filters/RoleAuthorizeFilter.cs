using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shared.Filters;

// ── USAGE ──────────────────────────────────────────────────────────────────
// Apply [RoleAuthorize] on controllers/actions that require specific roles.
// Replaces [Authorize(Roles = "...")] with a consistent JSON 403 response.
//
//   [RoleAuthorize(Roles.Admin)]
//   [RoleAuthorize(Roles.Admin, Roles.ComplianceOfficer)]  ← multiple roles
//
// This filter handles BOTH authentication AND role checks in one place:
//   • Not logged in        → 401 Unauthorized
//   • Wrong role           → 403 Forbidden
// ──────────────────────────────────────────────────────────────────────────

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RoleAuthorizeAttribute : TypeFilterAttribute
{
    public RoleAuthorizeAttribute(params string[] roles) : base(typeof(RoleAuthorizeFilter))
    {
        // Arguments are passed to RoleAuthorizeFilter's constructor by DI.
        Arguments = new object[] { roles };
    }
}

public class RoleAuthorizeFilter : IAuthorizationFilter
{
    private readonly string[] _roles;

    public RoleAuthorizeFilter(string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Check 1: is there a valid JWT at all?
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Authentication required."
            });
            return;
        }

        // Check 2: does the user's role match one of the required roles?
        // user.IsInRole() checks the ClaimTypes.Role claim set by UseAuthentication().
        if (!_roles.Any(role => user.IsInRole(role)))
        {
            context.Result = new ObjectResult(new
            {
                message = $"Access denied. Required role(s): {string.Join(", ", _roles)}."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
