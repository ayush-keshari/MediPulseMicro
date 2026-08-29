using NotificationService.Data;
using NotificationService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add centralized Serilog logging
builder.AddMediPulseSerilog();

// ── SHARED INFRASTRUCTURE ──────────────────────────────────────────────────
builder.Services.AddMediPulseControllers();
builder.Services.AddMediPulseSwagger("Notification Service");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediPulseCors();

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<INotificationService, NotificationServiceImpl>();
builder.Services.AddHealthChecks();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();

// Correct middleware order: Exception handling → CROS → Authentication → Authorization
app.UseMediPulseExceptionHandling();    // Handle exceptions from entire pipeline
app.UseMediPulseMiddleware();           // CORS → Authentication → Authorization

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
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
