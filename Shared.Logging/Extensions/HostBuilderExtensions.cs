using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Shared.Logging.Extensions;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog with JSON console output.
    /// Call this on the IHostBuilder in Program.cs before ConfigureWebHostDefaults.
    /// </summary>
    public static IHostBuilder UseSharedSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter());

            // Override noisy loggers
            loggerConfig
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);
        });
    }
}
