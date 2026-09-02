using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shared.Options;
using Shared.Services.Analytics;

namespace Shared.Tests.Services.Analytics;

[TestFixture]
[Category("Unit")]
public class PostHogCaptureSenderTests
{
    private const string ApiKey = "phc_test_key";
    private static readonly DateTimeOffset Timestamp = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly Exception? _failure;

        public FakeHandler(HttpStatusCode status = HttpStatusCode.OK, Exception? failure = null)
        {
            _status = status;
            _failure = failure;
        }

        public int Calls { get; private set; }
        public string? LastUrl { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUrl = request.RequestUri!.ToString();
            LastBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            if (_failure != null)
                throw _failure;

            return new HttpResponseMessage(_status);
        }
    }

    private static CaptureEnvelope Envelope(IReadOnlyDictionary<string, object?>? properties = null) =>
        new(AnalyticsEvents.ClubCreated,
            "3f6b1d3e-4b1a-4c1f-9f4a-0d2f6a1c5e77",
            properties ?? new Dictionary<string, object?>(),
            Timestamp);

    private static PostHogCaptureSender MakeSut(
        FakeHandler handler,
        RecordingLogger<PostHogCaptureSender> logger,
        string host = "https://eu.i.posthog.com")
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(PostHogCaptureSender.HttpClientName).Returns(new HttpClient(handler));

        var settings = Microsoft.Extensions.Options.Options.Create(new PostHogSettings { Host = host, ApiKey = ApiKey });
        return new PostHogCaptureSender(factory, settings, logger);
    }

    [Test]
    public async Task SendAsync_WithAnEvent_PostsTheCaptureBodyToTheCaptureEndpoint()
    {
        // Arrange
        var handler = new FakeHandler();
        var properties = new Dictionary<string, object?>
        {
            ["club_id"] = "9c2b4f5e-1111-2222-3333-444455556666",
            ["is_public"] = true,
            ["venue_count"] = 2,
            ["source"] = "server",
            ["client_source"] = ClientSource.Web
        };
        var sut = MakeSut(handler, new RecordingLogger<PostHogCaptureSender>());

        // Act
        var sent = await sut.SendAsync(Envelope(properties), CancellationToken.None);

        // Assert
        sent.Should().BeTrue();
        handler.LastUrl.Should().Be("https://eu.i.posthog.com/capture/");

        using var body = JsonDocument.Parse(handler.LastBody!);
        var root = body.RootElement;
        root.GetProperty("api_key").GetString().Should().Be(ApiKey);
        root.GetProperty("event").GetString().Should().Be(AnalyticsEvents.ClubCreated);
        root.GetProperty("distinct_id").GetString().Should().Be("3f6b1d3e-4b1a-4c1f-9f4a-0d2f6a1c5e77");
        root.GetProperty("timestamp").GetDateTimeOffset().Should().Be(Timestamp);

        var sentProperties = root.GetProperty("properties");
        sentProperties.GetProperty("club_id").GetString().Should().Be("9c2b4f5e-1111-2222-3333-444455556666");
        sentProperties.GetProperty("is_public").GetBoolean().Should().BeTrue();
        sentProperties.GetProperty("venue_count").GetInt32().Should().Be(2);
        sentProperties.GetProperty("source").GetString().Should().Be("server");
        sentProperties.GetProperty("client_source").GetString().Should().Be(ClientSource.Web);
    }

    [Test]
    public async Task SendAsync_WithAHostThatHasATrailingSlash_StillTargetsOneCapturePath()
    {
        // Arrange
        var handler = new FakeHandler();
        var sut = MakeSut(handler, new RecordingLogger<PostHogCaptureSender>(), "https://eu.i.posthog.com/");

        // Act
        await sut.SendAsync(Envelope(), CancellationToken.None);

        // Assert
        handler.LastUrl.Should().Be("https://eu.i.posthog.com/capture/");
    }

    [Test]
    public async Task SendAsync_WhenPostHogAnswersAnError_ReportsTheFailureWithoutThrowing()
    {
        // Arrange
        var handler = new FakeHandler(HttpStatusCode.InternalServerError);
        var sut = MakeSut(handler, new RecordingLogger<PostHogCaptureSender>());

        // Act
        var sent = await sut.SendAsync(Envelope(), CancellationToken.None);

        // Assert
        sent.Should().BeFalse();
    }

    [Test]
    public async Task SendAsync_WhenTheRequestThrows_SwallowsItAndLogsTheCause()
    {
        // Arrange
        var logger = new RecordingLogger<PostHogCaptureSender>();
        var handler = new FakeHandler(failure: new HttpRequestException("posthog is unreachable"));
        var sut = MakeSut(handler, logger);

        // Act
        var sent = await sut.SendAsync(Envelope(), CancellationToken.None);

        // Assert
        sent.Should().BeFalse();
        logger.Entries.Should().ContainSingle()
            .Which.Should().Match<(LogLevel Level, string Message, Exception? Exception)>(
                entry => entry.Level == LogLevel.Debug
                         && entry.Exception is HttpRequestException);
    }

    [Test]
    public async Task SendAsync_WhenTheServiceIsStopping_LetsTheCancellationOut()
    {
        // Arrange
        var handler = new FakeHandler(failure: new OperationCanceledException());
        var sut = MakeSut(handler, new RecordingLogger<PostHogCaptureSender>());
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        // Act
        var send = async () => await sut.SendAsync(Envelope(), stopping.Token);

        // Assert
        await send.Should().ThrowAsync<OperationCanceledException>();
    }
}
