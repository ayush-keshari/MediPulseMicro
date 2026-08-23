using LogisticsService.Data;
using LogisticsService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.ApplicationInsights;
using System;
using Shared.Middleware;

var loggerConfiguration = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .MinimumLevel.Information();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")))
{
    loggerConfiguration.WriteTo.ApplicationInsights(
        Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));
}

Log.Logger = loggerConfiguration.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ── SHARED INFRASTRUCTURE ─────────────────────────────────────────────────
builder.Services.AddMediPulseControllers();
builder.Services.AddMediPulseSwagger("Logistics Service");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediPulseCors();

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ILogisticsService, LogisticsServiceImpl>();
builder.Services.AddHealthChecks();

// ── SERILOG SETUP ────────────────────────────────────────────────────────
builder.Host.UseSerilog();

// ── BUILD & PIPELINE ──────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
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

app.UseMediPulseMiddleware();
app.UseMediPulseExceptionHandling();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
