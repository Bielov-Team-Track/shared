using Shared.Services.Embeds;

namespace Shared.Tests.Services.Embeds;

[TestFixture]
[Category("Unit")]
public class VideoUrlParserTests
{
    [TestCase("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [TestCase("https://youtu.be/dQw4w9WgXcQ")]
    [TestCase("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [TestCase("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [TestCase("https://www.youtube.com/watch?app=desktop&v=dQw4w9WgXcQ")]
    public void TryParse_WithYouTubeUrlForms_ExtractsCanonicalInfo(string url)
    {
        var ok = VideoUrlParser.TryParse(url, out var info);

        Assert.That(ok, Is.True);
        Assert.That(info!.Provider, Is.EqualTo("youtube"));
        Assert.That(info.EmbedId, Is.EqualTo("dQw4w9WgXcQ"));
        Assert.That(info.CanonicalUrl, Is.EqualTo("https://www.youtube.com/watch?v=dQw4w9WgXcQ"));
        Assert.That(info.EmbedUrl, Is.EqualTo("https://www.youtube.com/embed/dQw4w9WgXcQ"));
        Assert.That(info.ThumbnailUrl, Is.EqualTo("https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg"));
    }

    [TestCase("https://vimeo.com/76979871")]
    [TestCase("https://vimeo.com/video/76979871")]
    public void TryParse_WithVimeoUrlForms_ExtractsCanonicalInfo(string url)
    {
        var ok = VideoUrlParser.TryParse(url, out var info);

        Assert.That(ok, Is.True);
        Assert.That(info!.Provider, Is.EqualTo("vimeo"));
        Assert.That(info.EmbedId, Is.EqualTo("76979871"));
        Assert.That(info.CanonicalUrl, Is.EqualTo("https://vimeo.com/76979871"));
        Assert.That(info.EmbedUrl, Is.EqualTo("https://player.vimeo.com/video/76979871"));
        Assert.That(info.ThumbnailUrl, Is.EqualTo("https://vumbnail.com/76979871.jpg"));
    }

    [TestCase("https://example.com/watch?v=dQw4w9WgXcQ")]
    [TestCase("https://www.dailymotion.com/video/x7tgad0")]
    [TestCase("https://notvimeo.com/123456")]
    [TestCase("https://notyoutube.com/watch?v=dQw4w9WgXcQ")]
    [TestCase("not a url")]
    [TestCase("")]
    public void TryParse_WithUnsupportedUrl_ReturnsFalse(string url)
    {
        var ok = VideoUrlParser.TryParse(url, out var info);

        Assert.That(ok, Is.False);
        Assert.That(info, Is.Null);
    }
}
