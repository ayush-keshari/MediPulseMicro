using FacilityService.Data;
using FacilityService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

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

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMediPulseMiddleware();   // CORS → Authentication → Authorization
app.MapControllers();

app.Run();
