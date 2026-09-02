using Microsoft.Extensions.Logging;

namespace Shared.Services.Analytics;

/// <summary>
/// What every environment without a PostHog key gets: local development, CI and both test
/// suites. Nothing is queued, nothing is sent, and nothing needs stubbing to run the stack.
/// </summary>
public sealed class NoOpAnalyticsCapture : IAnalyticsCapture
{
    public NoOpAnalyticsCapture(ILogger<NoOpAnalyticsCapture> logger)
    {
        logger.LogInformation("PostHog analytics disabled: PostHog:ApiKey is not configured");
    }

    public void Capture(Guid userId, string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
    }
}
