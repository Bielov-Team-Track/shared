using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Enums;
using Shared.Services.Analytics;

namespace Shared.Tests.Services.Analytics;

[TestFixture]
[Category("Unit")]
public class PostHogAnalyticsCaptureTests
{
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private const string ServiceName = "clubs-service";

    private AnalyticsCaptureQueue _queue = null!;
    private IClientSourceAccessor _clientSourceAccessor = null!;
    private PostHogAnalyticsCapture _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _queue = new AnalyticsCaptureQueue();
        _clientSourceAccessor = Substitute.For<IClientSourceAccessor>();
        _clientSourceAccessor.Current.Returns(ClientSource.Unknown);

        _sut = new PostHogAnalyticsCapture(
            _queue, _clientSourceAccessor, new FakeTimeProvider(FrozenNow), ServiceName);
    }

    private CaptureEnvelope Queued() => _queue.DrainUpTo(10).Single();

    [Test]
    public void Capture_WithAnyEvent_StampsSourceClientSourceAndService()
    {
        // Arrange
        _clientSourceAccessor.Current.Returns(ClientSource.Web);

        // Act
        _sut.Capture(Guid.NewGuid(), AnalyticsEvents.ClubCreated);

        // Assert
        Queued().Properties.Should().Contain(new Dictionary<string, object?>
        {
            ["source"] = "server",
            ["client_source"] = ClientSource.Web,
            ["service"] = ServiceName
        });
    }

    [Test]
    public void Capture_WhenTheRequestDeclaresNoClient_RecordsClientSourceUnknown()
    {
        // Act
        _sut.Capture(Guid.NewGuid(), AnalyticsEvents.AccountCreated);

        // Assert
        Queued().Properties["client_source"].Should().Be(ClientSource.Unknown);
    }

    [Test]
    public void Capture_WhenACallSitePassesItsOwnSource_KeepsTheServerValue()
    {
        // Arrange
        var properties = new Dictionary<string, object?> { ["source"] = ClientSource.Web };

        // Act
        _sut.Capture(Guid.NewGuid(), AnalyticsEvents.ClubCreated, properties);

        // Assert
        Queued().Properties["source"].Should().Be("server");
    }

    [Test]
    public void Capture_WithAnEnumProperty_WritesItsLowerSnakeName()
    {
        // Arrange
        var properties = new Dictionary<string, object?> { ["age_tier"] = AgeTier.TeenConsentTo17 };

        // Act
        _sut.Capture(Guid.NewGuid(), AnalyticsEvents.AccountCreated, properties);

        // Assert
        Queued().Properties["age_tier"].Should().Be("teen_consent_to17");
    }

    [Test]
    public void Capture_WithNonEnumProperties_PassesThemThroughUntouched()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var properties = new Dictionary<string, object?>
        {
            ["club_id"] = clubId,
            ["is_public"] = true,
            ["venue_count"] = 2,
            ["event_id"] = null
        };

        // Act
        _sut.Capture(Guid.NewGuid(), AnalyticsEvents.ClubCreated, properties);

        // Assert
        var queued = Queued().Properties;
        queued["club_id"].Should().Be(clubId);
        queued["is_public"].Should().Be(true);
        queued["venue_count"].Should().Be(2);
        queued["event_id"].Should().BeNull();
    }

    [Test]
    public void Capture_WithAUserId_UsesItAsDistinctIdAndStampsTheInjectedClock()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        _sut.Capture(userId, AnalyticsEvents.EmailVerified);

        // Assert
        var queued = Queued();
        queued.Event.Should().Be(AnalyticsEvents.EmailVerified);
        queued.DistinctId.Should().Be(userId.ToString());
        queued.Timestamp.Should().Be(FrozenNow);
    }

    [Test]
    public void Capture_WhenTheQueueIsFull_DropsTheEventWithoutThrowing()
    {
        // Arrange
        for (var i = 0; i < AnalyticsCaptureQueue.Capacity; i++)
            _sut.Capture(Guid.NewGuid(), AnalyticsEvents.InvitationSent);

        // Act
        var capture = () => _sut.Capture(Guid.NewGuid(), AnalyticsEvents.InvitationSent);

        // Assert
        capture.Should().NotThrow();
        _queue.TakeDroppedCount().Should().Be(1);
    }
}
