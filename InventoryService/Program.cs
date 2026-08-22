using InventoryService.Data;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Serilog;
// Services registered: IInventoryService, IExceptionService, IReplenishmentService

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .MinimumLevel.Information()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ── SHARED INFRASTRUCTURE ─────────────────────────────────────────────────
builder.Services.AddMediPulseControllers();
builder.Services.AddMediPulseSwagger("Inventory Service");
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediPulseCors();

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IInventoryService, InventoryServiceImpl>();
builder.Services.AddScoped<IExceptionService, ExceptionServiceImpl>();
builder.Services.AddScoped<IReplenishmentService, ReplenishmentServiceImpl>();

// ── SERILOG SETUP ────────────────────────────────────────────────
builder.Host.UseSerilog();

// ── BUILD & PIPELINE ──────────────────────────────────────────────────────
var app = builder.Build();

// Auto-create / migrate database on startup (retry handles concurrent-start race on shared DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
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
