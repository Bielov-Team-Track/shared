using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shared.Options;
using Shared.Services.Analytics;

namespace Shared.Tests.Services.Analytics;

[TestFixture]
[Category("Unit")]
public class PostHogCaptureDispatcherTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(5);

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private int _calls;

        public CountingHandler(HttpStatusCode status)
        {
            _status = status;
        }

        public int Calls => Volatile.Read(ref _calls);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }

    private AnalyticsCaptureQueue _queue = null!;
    private RecordingLogger<PostHogCaptureDispatcher> _logger = null!;
    private PostHogCaptureDispatcher _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _queue = new AnalyticsCaptureQueue();
        _logger = new RecordingLogger<PostHogCaptureDispatcher>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.StopAsync(CancellationToken.None);
        _sut.Dispose();
    }

    private void GivenPostHogAnswers(HttpStatusCode status, out CountingHandler handler)
    {
        handler = new CountingHandler(status);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(PostHogCaptureSender.HttpClientName).Returns(new HttpClient(handler));

        var sender = new PostHogCaptureSender(
            factory,
            Microsoft.Extensions.Options.Options.Create(new PostHogSettings { Host = "https://eu.i.posthog.com", ApiKey = "phc_test_key" }),
            new RecordingLogger<PostHogCaptureSender>());

        _sut = new PostHogCaptureDispatcher(_queue, sender, _logger);
    }

    private void Enqueue(string eventName) =>
        _queue.TryEnqueue(new CaptureEnvelope(
            eventName, Guid.NewGuid().ToString(), new Dictionary<string, object?>(), DateTimeOffset.UnixEpoch));

    private static async Task WaitUntilAsync(Func<bool> condition, string what)
    {
        var deadline = TimeProvider.System.GetUtcNow() + WaitTimeout;
        while (!condition())
        {
            if (TimeProvider.System.GetUtcNow() > deadline)
                Assert.Fail($"Timed out waiting for {what}");

            await Task.Delay(PollDelay);
        }
    }

    [Test]
    public async Task ExecuteAsync_WithQueuedEvents_SendsEveryOneAndLogsNothing()
    {
        // Arrange
        GivenPostHogAnswers(HttpStatusCode.OK, out var handler);
        Enqueue(AnalyticsEvents.ClubCreated);
        Enqueue(AnalyticsEvents.InvitationSent);

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => handler.Calls >= 2, "both events to be sent");

        // Assert
        _logger.Entries.Should().BeEmpty();
    }

    [Test]
    public async Task ExecuteAsync_WhenPostHogIsFailing_KeepsDrainingAndWarnsOncePerBatch()
    {
        // Arrange
        GivenPostHogAnswers(HttpStatusCode.InternalServerError, out var handler);
        Enqueue(AnalyticsEvents.ClubCreated);
        Enqueue(AnalyticsEvents.InvitationSent);

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => handler.Calls >= 2, "both events to be attempted");
        await WaitUntilAsync(() => _logger.Entries.Count > 0, "the failure to be reported");

        // Assert — one line for the pass, not one per lost event
        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Warning);
        _logger.Entries[0].Message.Should().Contain("2 of 2 failed to send");
    }

    [Test]
    public async Task ExecuteAsync_AfterEventsWereDroppedByAFullQueue_ReportsTheDropCount()
    {
        // Arrange
        GivenPostHogAnswers(HttpStatusCode.OK, out _);
        for (var i = 0; i < AnalyticsCaptureQueue.Capacity + 3; i++)
            Enqueue(AnalyticsEvents.InvitationSent);

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await WaitUntilAsync(
            () => _logger.Entries.Any(e => e.Level == LogLevel.Warning),
            "the drop count to be reported");

        // Assert
        _logger.Entries.Should().Contain(e => e.Message.Contains("3 dropped by a full queue"));
    }
}
