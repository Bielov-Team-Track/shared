using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Services.Embeds;

namespace Shared.Tests.Services.Embeds;

[TestFixture]
[Category("Unit")]
public class VideoOEmbedClientTests
{
    private static readonly VideoEmbedInfo YouTubeInfo = new(
        "youtube", "dQw4w9WgXcQ",
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        "https://www.youtube.com/embed/dQw4w9WgXcQ",
        "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg");

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        public int Calls { get; private set; }
        public string? LastUrl { get; private set; }

        public FakeHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastUrl = request.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private IDistributedCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = Substitute.For<IDistributedCache>();
    }

    private VideoOEmbedClient MakeSut(FakeHandler handler) =>
        new(new HttpClient(handler), _cache, NullLogger<VideoOEmbedClient>.Instance);

    [Test]
    public async Task GetAsync_WithSuccessfulResponse_ParsesTitleAndThumbnailAndCaches()
    {
        var handler = new FakeHandler(HttpStatusCode.OK,
            """{"title":"Never Gonna Give You Up","thumbnail_url":"https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"}""");
        var sut = MakeSut(handler);

        var result = await sut.GetAsync(YouTubeInfo);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Never Gonna Give You Up"));
        Assert.That(result.ThumbnailUrl, Is.EqualTo("https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"));
        Assert.That(handler.LastUrl, Does.StartWith("https://www.youtube.com/oembed?"));
        await _cache.Received(1).SetAsync(
            "oembed:youtube:dQw4w9WgXcQ",
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_WithCachedEntry_SkipsHttp()
    {
        var cached = Encoding.UTF8.GetBytes(
            """{"Title":"Cached Title","ThumbnailUrl":"https://thumb.test/x.jpg"}""");
        _cache.GetAsync("oembed:youtube:dQw4w9WgXcQ", Arg.Any<CancellationToken>())
            .Returns(cached);
        var handler = new FakeHandler(HttpStatusCode.OK, "{}");
        var sut = MakeSut(handler);

        var result = await sut.GetAsync(YouTubeInfo);

        Assert.That(result!.Title, Is.EqualTo("Cached Title"));
        Assert.That(handler.Calls, Is.Zero);
    }

    [Test]
    public async Task GetAsync_WithHttpFailure_ReturnsNullAndDoesNotCache()
    {
        var handler = new FakeHandler(HttpStatusCode.InternalServerError, "nope");
        var sut = MakeSut(handler);

        var result = await sut.GetAsync(YouTubeInfo);

        Assert.That(result, Is.Null);
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_WithMalformedJson_ReturnsNull()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "<html>not json</html>");
        var sut = MakeSut(handler);

        var result = await sut.GetAsync(YouTubeInfo);

        Assert.That(result, Is.Null);
    }
}
