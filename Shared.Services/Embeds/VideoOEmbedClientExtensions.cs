namespace Shared.Services.Embeds;

public static class VideoOEmbedClientExtensions
{
    /// <summary>
    /// Resolves display metadata for an embed: oEmbed values when available,
    /// otherwise the provider template thumbnail and no title.
    /// </summary>
    public static async Task<(string ThumbnailUrl, string? Title)> ResolveDisplayAsync(
        this IVideoOEmbedClient client, VideoEmbedInfo embed, CancellationToken cancellationToken = default)
    {
        var oEmbed = await client.GetAsync(embed, cancellationToken);
        return (oEmbed?.ThumbnailUrl ?? embed.ThumbnailUrl, oEmbed?.Title);
    }
}
