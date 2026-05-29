using AuditService.Data;
using AuditService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── SHARED INFRASTRUCTURE ──────────────────────────────────────────────────
builder.Services.AddMediPulseControllers();
builder.Services.AddMediPulseSwagger("Audit Service");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediPulseCors();

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
// Points to MedipulseAudit — separate from MedipulseMain used by all other services.
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuditLogService, AuditLogServiceImpl>();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();
app.MigrateDatabase<AuditDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMediPulseMiddleware();
app.MapControllers();

app.Run();
