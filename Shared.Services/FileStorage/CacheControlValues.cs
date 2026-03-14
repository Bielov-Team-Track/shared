namespace Shared.Services.FileStorage;

public static class CacheControlValues
{
    /// <summary>
    /// For content with immutable keys (posts, drills). Cached for 1 year.
    /// </summary>
    public const string Immutable = "public, max-age=31536000, immutable";

    /// <summary>
    /// For mutable content (avatars). Short browser cache (5 min), long CDN cache (24h).
    /// </summary>
    public const string Mutable = "public, max-age=300, s-maxage=86400";

    /// <summary>
    /// For sensitive content (payment evidence, private attachments). Never cached.
    /// </summary>
    public const string NoStore = "no-store";
}
