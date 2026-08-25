using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System;
using System.IO;

using Serilog;
using Serilog.Sinks.ApplicationInsights;
using Shared.Extensions;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Gateway.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add centralized Serilog logging
builder.AddMediPulseSerilog();

// Load ocelot.json alongside appsettings.json.
// Ocelot reads all route definitions from this file.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// CORS — Angular (port 4200) talks to Gateway (port 5000).
// Only the Gateway needs CORS since it's the single entry point for the frontend.
builder.Services.AddMediPulseCors();

// Register Ocelot — reads ocelot.json and sets up the reverse proxy routing.
builder.Services.AddOcelot();
builder.Services.AddHealthChecks();

// ── SERILOG SETUP ────────────────────────────────────────────
// Already called via AddMediPulseSerilog()
// builder.Host.UseSerilog();

var app = builder.Build();

app.UseCors("AllowAngular");
app.MapHealthChecks("/health");

// UseOcelot() is the middleware that intercepts every incoming request,
// matches it against ocelot.json routes, and forwards it to the correct service.
// This call is async and must be awaited — it replaces app.Run() internally.
await app.UseOcelot();

app.Run();

public partial class Program { }