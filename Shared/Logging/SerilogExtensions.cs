using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

namespace Shared.Logging
{
    /// <summary>
    /// Extension methods to add Serilog with JSON console sink to the logging pipeline.
    /// </summary>
    public static class SerilogExtensions
    {
        /// <summary>
        /// Adds Serilog as the logging provider for the application.
        /// Reads configuration from the provided IConfiguration (optional).
        /// Outputs JSON-formatted logs to the console.
        /// </summary>
        public static ILoggingBuilder AddMediPulseSerilog(this ILoggingBuilder builder, IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new JsonFormatter());

            if (configuration != null)
            {
                loggerConfiguration.ReadFrom.Configuration(configuration);
            }

            Log.Logger = loggerConfiguration.CreateLogger();

            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);

            return builder;
        }
    }
}
