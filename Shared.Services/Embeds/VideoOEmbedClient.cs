using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Shared.Services.Embeds;

public sealed class VideoOEmbedClient : IVideoOEmbedClient
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    private readonly HttpClient _http;
    private readonly IDistributedCache _cache;
    private readonly ILogger<VideoOEmbedClient> _logger;

    public VideoOEmbedClient(HttpClient http, IDistributedCache cache, ILogger<VideoOEmbedClient> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<VideoOEmbedResult?> GetAsync(VideoEmbedInfo embed, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"oembed:{embed.Provider}:{embed.EmbedId}";
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cached))
                return JsonSerializer.Deserialize<VideoOEmbedResult>(cached);

            var endpoint = embed.Provider switch
            {
                "youtube" => $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(embed.CanonicalUrl)}&format=json",
                "vimeo" => $"https://vimeo.com/api/oembed.json?url={Uri.EscapeDataString(embed.CanonicalUrl)}",
                _ => null,
            };
            if (endpoint == null)
                return null;

            using var response = await _http.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = json.RootElement;
            var result = new VideoOEmbedResult(
                Title: root.TryGetProperty("title", out var title) ? title.GetString() : null,
                ThumbnailUrl: root.TryGetProperty("thumbnail_url", out var thumb) ? thumb.GetString() : null);

            // Failures are NOT cached — they are usually transient.
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "oEmbed lookup failed for {Provider}:{EmbedId}", embed.Provider, embed.EmbedId);
            return null;
        }
    }
}
