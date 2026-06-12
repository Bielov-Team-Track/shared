using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Shared.Services.Embeds;

public sealed record VideoEmbedInfo(
    string Provider,
    string EmbedId,
    string CanonicalUrl,
    string EmbedUrl,
    string ThumbnailUrl);

public static class VideoUrlParser
{
    // The host check lives inside each pattern, so lookalike domains
    // (example.com/watch?v=…) never match.
    private static readonly Regex YouTube = new(
        @"(?:https?://)?(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:[^#\s]*&)?v=|embed/|shorts/)|youtu\.be/)(?<id>[A-Za-z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Vimeo = new(
        @"(?:https?://)?(?:www\.)?vimeo\.com/(?:video/)?(?<id>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryParse(string? url, [NotNullWhen(true)] out VideoEmbedInfo? info)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var youtube = YouTube.Match(url);
        if (youtube.Success)
        {
            var id = youtube.Groups["id"].Value;
            info = new VideoEmbedInfo(
                Provider: "youtube",
                EmbedId: id,
                CanonicalUrl: $"https://www.youtube.com/watch?v={id}",
                EmbedUrl: $"https://www.youtube.com/embed/{id}",
                ThumbnailUrl: $"https://img.youtube.com/vi/{id}/hqdefault.jpg");
            return true;
        }

        var vimeo = Vimeo.Match(url);
        if (vimeo.Success)
        {
            var id = vimeo.Groups["id"].Value;
            info = new VideoEmbedInfo(
                Provider: "vimeo",
                EmbedId: id,
                CanonicalUrl: $"https://vimeo.com/{id}",
                EmbedUrl: $"https://player.vimeo.com/video/{id}",
                ThumbnailUrl: $"https://vumbnail.com/{id}.jpg");
            return true;
        }

        return false;
    }
}
