using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load ocelot.json alongside appsettings.json.
// Ocelot reads all route definitions from this file.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// CORS — Angular (port 4200) talks to Gateway (port 5000).
// Only the Gateway needs CORS since it's the single entry point for the frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Register Ocelot — reads ocelot.json and sets up the reverse proxy routing.
builder.Services.AddOcelot();

var app = builder.Build();

app.UseCors("AllowAngular");

// UseOcelot() is the middleware that intercepts every incoming request,
// matches it against ocelot.json routes, and forwards it to the correct service.
// This call is async and must be awaited — it replaces app.Run() internally.
await app.UseOcelot();

app.Run();
