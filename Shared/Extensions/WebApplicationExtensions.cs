using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions;

// Extension methods on WebApplication (the built app, not the builder).
// Packages the middleware pipeline order that every service needs.
// Middleware ORDER is critical in ASP.NET Core — changing the order changes behaviour.
public static class WebApplicationExtensions
{
    // Correct order: CORS → Authentication → Authorization
    //
    // Why this order?
    //   CORS must run first so browsers get the right headers even on 401/403 responses.
    //   Authentication runs next to decode the JWT and populate HttpContext.User.
    //   Authorization runs after so it can read the populated User identity.
    //
    // Every service calls this ONE line:
    //   app.UseMediPulseMiddleware();
    public static WebApplication UseMediPulseMiddleware(this WebApplication app)
    {
        app.UseCors("AllowAngular");
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    // Apply pending EF Core migrations on startup.
    // Why: in Docker the DB starts empty, so each service must create its own schema before serving requests.
    public static WebApplication MigrateDatabase<TContext>(this WebApplication app) where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        db.Database.Migrate();
        return app;
    }
}
