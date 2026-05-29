using AuthService.Data;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── SHARED INFRASTRUCTURE (one line each — logic lives in Shared) ──────────
builder.Services.AddMediPulseControllers();                         // controllers + 3 global filters
builder.Services.AddMediPulseSwagger("Auth Service");               // Swagger UI with JWT Authorize button
builder.Services.AddJwtAuthentication(builder.Configuration);       // JWT Bearer middleware
builder.Services.AddMediPulseCors();                                // CORS for Angular port 4200

// ── SERVICE-SPECIFIC REGISTRATIONS ────────────────────────────────────────
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthServiceImpl>();

// ── BUILD & PIPELINE ───────────────────────────────────────────────────────
var app = builder.Build();
app.MigrateDatabase<AuthDbContext>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMediPulseMiddleware();   // CORS → Authentication → Authorization (order matters)
app.MapControllers();

app.Run();
