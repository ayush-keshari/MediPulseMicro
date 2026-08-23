using FacilityService.Data;
using FacilityService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Serilog;
using System;
using Serilog.Sinks.ApplicationInsights;

var loggerConfiguration = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .MinimumLevel.Information();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")))
{
    loggerConfiguration.WriteTo.ApplicationInsights(
        Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));
}

Log.Logger = loggerConfiguration.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ── SHARED INFRASTRUCTURE (one line each — logic lives in Shared) ──────────
builder.Services.AddMediPulseControllers();                         // controllers + 3 global filters
builder.Services.AddMediPulseSwagger("Facility Service");           // Swagger UI with JWT Authorize button
builder.Services.AddJwtAuthentication(builder.Configuration);       // JWT Bearer middleware
builder.Services.AddMediPulseCors();                                // CORS for Angular port 4200

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<FacilityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFacilityService, FacilityServiceImpl>();
builder.Services.AddHealthChecks();

// ── SERILOG SETUP ────────────────────────────────────────────────
builder.Host.UseSerilog();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FacilityDbContext>();
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

app.UseMediPulseMiddleware();   // CORS → Authentication → Authorization
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
