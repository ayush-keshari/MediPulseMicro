using Microsoft.EntityFrameworkCore;
using ProcurementService.Data;
using ProcurementService.Services;
using Shared.Extensions;
using Serilog;
using Serilog.Sinks.ApplicationInsights;
using System;

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

// ── SHARED INFRASTRUCTURE ──────────────────────────────────────────────────
builder.Services.AddMediPulseControllers();
builder.Services.AddMediPulseSwagger("Procurement Service");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediPulseCors();

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<ProcurementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProcurementService, ProcurementServiceImpl>();
builder.Services.AddHealthChecks();

// ── SERILOG SETUP ────────────────────────────────────────────────
builder.Host.UseSerilog();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
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
app.MapControllers();

app.Run();
