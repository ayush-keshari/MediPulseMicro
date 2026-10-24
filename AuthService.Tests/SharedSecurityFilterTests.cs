using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Filters;

namespace AuthService.Tests;

public class SharedSecurityFilterTests
{
    [Fact]
    public void JwtAuthFilter_RejectsAnonymousAndAllowsAuthenticatedUsers()
    {
        var anonymous = CreateAuthorizationContext(new ClaimsPrincipal(new ClaimsIdentity()));
        new JwtAuthFilter().OnAuthorization(anonymous);
        Assert.IsType<UnauthorizedObjectResult>(anonymous.Result);

        var authenticated = CreateAuthorizationContext(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "test")));
        new JwtAuthFilter().OnAuthorization(authenticated);
        Assert.Null(authenticated.Result);
    }

    [Fact]
    public void RoleAuthorizeFilter_Returns401ForAnonymous_403ForWrongRole_AndAllowsMatchingRole()
    {
        var anonymous = CreateAuthorizationContext(new ClaimsPrincipal(new ClaimsIdentity()));
        new RoleAuthorizeFilter(["Admin"]).OnAuthorization(anonymous);
        Assert.IsType<UnauthorizedObjectResult>(anonymous.Result);

        var wrongRole = CreateAuthorizationContext(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "Nurse")], "test")));
        new RoleAuthorizeFilter(["Admin"]).OnAuthorization(wrongRole);
        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(wrongRole.Result).StatusCode);

        var allowed = CreateAuthorizationContext(new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test")));
        new RoleAuthorizeFilter(["Admin", "ComplianceOfficer"]).OnAuthorization(allowed);
        Assert.Null(allowed.Result);
    }

    [Fact]
    public void ValidationFilter_ReturnsConsistentBadRequestWithFieldErrors()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        actionContext.ModelState.AddModelError("Email", "The Email field is required.");
        var context = new ActionExecutingContext(
            actionContext,
            Array.Empty<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        new ValidationFilter().OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode ?? StatusCodes.Status400BadRequest);
        Assert.Contains("Validation failed", result.Value!.ToString());
    }

    [Fact]
    public void GlobalExceptionFilter_ConvertsUnhandledExceptionToJson500()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var context = new ExceptionContext(actionContext, Array.Empty<IFilterMetadata>())
        {
            Exception = new InvalidOperationException("database unavailable")
        };

        new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance).OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.True(context.ExceptionHandled);
        Assert.DoesNotContain("database unavailable", result.Value!.ToString());
        Assert.Contains("INTERNAL_SERVER_ERROR", result.Value!.ToString());
    }

    private static AuthorizationFilterContext CreateAuthorizationContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, Array.Empty<IFilterMetadata>());
    }
}
