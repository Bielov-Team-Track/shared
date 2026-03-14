using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Logging.Extensions;

public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Configures Sentry error tracking on the web host.
    /// Reads DSN from Configuration["Sentry:Dsn"]. If no DSN is set, Sentry is disabled.
    /// Call this in ConfigureWebHostDefaults alongside UseStartup.
    /// </summary>
    public static IWebHostBuilder UseSharedSentry(this IWebHostBuilder builder)
    {
        return builder.UseSentry(options =>
        {
            // Default to empty string (disables Sentry) when no DSN is configured.
            // This prevents ArgumentNullException in test/local environments.
            if (string.IsNullOrEmpty(options.Dsn))
            {
                options.Dsn = "";
                return;
            }

            options.SendDefaultPii = false;
            options.AttachStacktrace = true;
            options.MinimumBreadcrumbLevel = LogLevel.Information;
            options.MinimumEventLevel = LogLevel.Error;
            options.TracesSampleRate = 0.2;
        });
    }
}
