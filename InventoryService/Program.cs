using InventoryService.Data;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
// Services registered: IInventoryService, IExceptionService, IReplenishmentService

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

// ── BUILD & PIPELINE ──────────────────────────────────────────────────────
var app = builder.Build();
app.MigrateDatabase<InventoryDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMediPulseMiddleware();
app.MapControllers();
app.Run();
