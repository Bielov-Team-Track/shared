using NSubstitute;
using Shared.Services.Embeds;

namespace Shared.Tests.Services.Embeds;

[TestFixture]
[Category("Unit")]
public class VideoOEmbedClientExtensionsTests
{
    private static readonly VideoEmbedInfo Info = new(
        "vimeo", "76979871",
        "https://vimeo.com/76979871",
        "https://player.vimeo.com/video/76979871",
        "https://vumbnail.com/76979871.jpg");

    [Test]
    public async Task ResolveDisplayAsync_WithOEmbedData_ReturnsOEmbedValues()
    {
        var client = Substitute.For<IVideoOEmbedClient>();
        client.GetAsync(Info, Arg.Any<CancellationToken>())
            .Returns(new VideoOEmbedResult("The Mountain", "https://i.vimeocdn.com/big.jpg"));

        var (thumbnailUrl, title) = await client.ResolveDisplayAsync(Info);

        Assert.That(thumbnailUrl, Is.EqualTo("https://i.vimeocdn.com/big.jpg"));
        Assert.That(title, Is.EqualTo("The Mountain"));
    }

    [Test]
    public async Task ResolveDisplayAsync_WhenOEmbedUnavailable_FallsBackToTemplate()
    {
        var client = Substitute.For<IVideoOEmbedClient>();
        client.GetAsync(Info, Arg.Any<CancellationToken>())
            .Returns((VideoOEmbedResult?)null);

        var (thumbnailUrl, title) = await client.ResolveDisplayAsync(Info);

        Assert.That(thumbnailUrl, Is.EqualTo("https://vumbnail.com/76979871.jpg"));
        Assert.That(title, Is.Null);
    }
}
