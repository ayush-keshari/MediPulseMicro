// ── CORS ──────────────────────────────────────────────────────────────
    // Allows Angular (port 4200) to call the Gateway/services during development.
    // Every service calls this ONE line:
    //   builder.Services.AddMediPulseCors();
    public static IServiceCollection AddMediPulseCors(
        this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
                policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }