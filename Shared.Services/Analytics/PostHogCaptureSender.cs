using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Shared.Services.Analytics;

public sealed class PostHogCaptureSender
{
    public const string HttpClientName = "posthog-analytics";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PostHogCaptureSender> _logger;
    private readonly string _captureUrl;
    private readonly string _apiKey;

    public PostHogCaptureSender(
        IHttpClientFactory httpClientFactory,
        IOptions<PostHogSettings> settings,
        ILogger<PostHogCaptureSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _captureUrl = $"{settings.Value.Host.TrimEnd('/')}/capture/";
        _apiKey = settings.Value.ApiKey!;
    }

    /// <summary>
    /// Posts one event. Never throws for a PostHog failure: the caller is a background drain
    /// whose only job is to keep draining.
    /// </summary>
    public async Task<bool> SendAsync(CaptureEnvelope envelope, CancellationToken cancellationToken)
    {
        var body = new CaptureBody(_apiKey, envelope.Event, envelope.DistinctId,
            envelope.Properties, envelope.Timestamp);

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(_captureUrl, body, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Debug, not Warning: the dispatcher reports the count once per drain pass, and a
            // PostHog outage would otherwise write one warning per event.
            _logger.LogDebug(ex, "PostHog capture of {Event} failed", envelope.Event);
            return false;
        }
    }

    private sealed record CaptureBody(
        [property: JsonPropertyName("api_key")] string ApiKey,
        [property: JsonPropertyName("event")] string Event,
        [property: JsonPropertyName("distinct_id")] string DistinctId,
        [property: JsonPropertyName("properties")] IReadOnlyDictionary<string, object?> Properties,
        [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);
}
