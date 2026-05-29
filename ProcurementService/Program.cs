using Microsoft.EntityFrameworkCore;
using ProcurementService.Data;
using ProcurementService.Services;
using Shared.Extensions;

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

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();
app.MigrateDatabase<ProcurementDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMediPulseMiddleware();
app.MapControllers();

app.Run();
