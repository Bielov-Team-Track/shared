namespace Shared.Services.Embeds;

public sealed record VideoOEmbedResult(string? Title, string? ThumbnailUrl);

public interface IVideoOEmbedClient
{
    /// <summary>
    /// Best-effort oEmbed lookup for a parsed video embed. Returns null on ANY
    /// failure (timeout, non-2xx, bad JSON) — callers fall back to the provider
    /// template thumbnail and no title. Successes are cached for 7 days.
    /// </summary>
    Task<VideoOEmbedResult?> GetAsync(VideoEmbedInfo embed, CancellationToken cancellationToken = default);
}
