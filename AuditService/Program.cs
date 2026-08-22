using AuditService.Data;
using AuditService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

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

// ── SERILOG SETUP ────────────────────────────────────────────────
builder.Host.UseSerilog();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
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
