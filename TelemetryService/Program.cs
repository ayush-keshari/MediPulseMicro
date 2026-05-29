using Microsoft.EntityFrameworkCore;
using TelemetryService.Data;
using TelemetryService.Services;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── SHARED INFRASTRUCTURE ──────────────────────────────────────────────────
builder.Services.AddMediPulseControllers();
builder.Services.AddMediPulseSwagger("Telemetry Service");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediPulseCors();

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITelemetryService, TelemetryServiceImpl>();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();
app.MigrateDatabase<TelemetryDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMediPulseMiddleware();
app.MapControllers();

app.Run();
