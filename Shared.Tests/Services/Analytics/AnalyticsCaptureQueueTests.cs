using FluentAssertions;
using Shared.Services.Analytics;

namespace Shared.Tests.Services.Analytics;

[TestFixture]
[Category("Unit")]
public class AnalyticsCaptureQueueTests
{
    private AnalyticsCaptureQueue _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new AnalyticsCaptureQueue();
    }

    private static CaptureEnvelope Envelope(string name = AnalyticsEvents.ClubCreated) =>
        new(name, Guid.NewGuid().ToString(), new Dictionary<string, object?>(), DateTimeOffset.UnixEpoch);

    private void Fill()
    {
        for (var i = 0; i < AnalyticsCaptureQueue.Capacity; i++)
            _sut.TryEnqueue(Envelope());
    }

    [Test]
    public void TryEnqueue_WithRoomLeft_AcceptsTheEvent()
    {
        // Act
        var accepted = _sut.TryEnqueue(Envelope());

        // Assert
        accepted.Should().BeTrue();
        _sut.TakeDroppedCount().Should().Be(0);
    }

    [Test]
    public void TryEnqueue_WhenTheQueueIsFull_DropsTheEventAndCountsIt()
    {
        // Arrange
        Fill();

        // Act
        var accepted = _sut.TryEnqueue(Envelope(AnalyticsEvents.InvitationSent));

        // Assert
        accepted.Should().BeFalse();
        _sut.TakeDroppedCount().Should().Be(1);
    }

    [Test]
    public void TakeDroppedCount_AfterBeingRead_StartsCountingAgainFromZero()
    {
        // Arrange
        Fill();
        _sut.TryEnqueue(Envelope());
        _sut.TryEnqueue(Envelope());

        // Act
        var first = _sut.TakeDroppedCount();
        var second = _sut.TakeDroppedCount();

        // Assert
        first.Should().Be(2);
        second.Should().Be(0);
    }

    [Test]
    public void DrainUpTo_WithMoreQueuedThanAsked_LeavesTheRemainderForTheNextPass()
    {
        // Arrange
        _sut.TryEnqueue(Envelope(AnalyticsEvents.ClubCreated));
        _sut.TryEnqueue(Envelope(AnalyticsEvents.InvitationSent));
        _sut.TryEnqueue(Envelope(AnalyticsEvents.InvitationAccepted));

        // Act
        var first = _sut.DrainUpTo(2);
        var second = _sut.DrainUpTo(2);

        // Assert
        first.Select(e => e.Event).Should()
            .Equal(AnalyticsEvents.ClubCreated, AnalyticsEvents.InvitationSent);
        second.Select(e => e.Event).Should().Equal(AnalyticsEvents.InvitationAccepted);
    }

    [Test]
    public void DrainUpTo_WithAnEmptyQueue_ReturnsNothing()
    {
        // Act
        var batch = _sut.DrainUpTo(10);

        // Assert
        batch.Should().BeEmpty();
    }
}
