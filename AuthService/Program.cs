using AuthService.Data;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add centralized Serilog logging
builder.AddMediPulseSerilog();

// ── SHARED INFRASTRUCTURE (one line each — logic lives in Shared) ──────────
builder.Services.AddMediPulseControllers();                         // controllers + 3 global filters
builder.Services.AddMediPulseSwagger("Auth Service");               // Swagger UI with JWT Authorize button
builder.Services.AddJwtAuthentication(builder.Configuration);       // JWT Bearer middleware
builder.Services.AddMediPulseCors();                                // CORS for Angular port 4200

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthServiceImpl>();
builder.Services.AddHealthChecks();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();

// Correct middleware order: Exception handling → CORS → Authentication → Authorization
app.UseMediPulseExceptionHandling();    // Handle exceptions from entire pipeline
app.UseMediPulseMiddleware();           // CORS → Authentication → Authorization

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    for (var attempt = 0; attempt < 5; attempt++)
    {
        try { db.Database.Migrate(); break; }
        catch when (attempt < 4) { Thread.Sleep(2000); }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }