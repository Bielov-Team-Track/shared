using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Options;

namespace Shared.Services.Analytics;

public static class PostHogAnalyticsExtensions
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Wires the server-owned conversion events for one service. Without a configured key the
    /// whole feature is a no-op, so nothing outside the deployed environments needs a PostHog
    /// project — or a stub — to run.
    /// </summary>
    /// <param name="serviceName">Stamped on every event as <c>service</c>, e.g. "auth-service".</param>
    public static IServiceCollection AddPostHogAnalytics(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var section = configuration.GetSection(PostHogSettings.SectionName);

        if (string.IsNullOrWhiteSpace(section[nameof(PostHogSettings.ApiKey)]))
        {
            services.AddSingleton<IAnalyticsCapture, NoOpAnalyticsCapture>();
            return services;
        }

        services.AddOptions<PostHogSettings>()
            .BindConfiguration(PostHogSettings.SectionName)
            .Validate(s => Uri.TryCreate(s.Host, UriKind.Absolute, out _),
                "PostHog:Host must be a valid absolute URL");

        services.AddHttpContextAccessor();
        services.AddHttpClient(PostHogCaptureSender.HttpClientName, client => client.Timeout = RequestTimeout);

        // The generic host does not register a clock, and this branch is the only one that needs
        // one — so a service that happened to get TimeProvider from another package's TryAdd would
        // fail here first in whichever environment has a key, which is never a developer's.
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IClientSourceAccessor, ClientSourceAccessor>();
        services.AddSingleton<AnalyticsCaptureQueue>();
        services.AddSingleton<PostHogCaptureSender>();
        services.AddSingleton<IAnalyticsCapture>(sp => new PostHogAnalyticsCapture(
            sp.GetRequiredService<AnalyticsCaptureQueue>(),
            sp.GetRequiredService<IClientSourceAccessor>(),
            sp.GetRequiredService<TimeProvider>(),
            serviceName));
        services.AddHostedService<PostHogCaptureDispatcher>();

        return services;
    }
}
