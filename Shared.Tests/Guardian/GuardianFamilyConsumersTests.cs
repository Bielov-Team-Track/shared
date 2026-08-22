using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Enums;
using Shared.Guardian.Consumers;
using Shared.Guardian.Interfaces;
using Shared.Messaging.Contracts.Events.Family;

namespace Shared.Tests.Guardian;

[TestFixture]
[Category("Unit")]
public class GuardianFamilyConsumersTests
{
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private FakeTimeProvider _timeProvider = null!;
    private IGuardianLinkService _linkService = null!;

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    [SetUp]
    public void SetUp()
    {
        _timeProvider = new FakeTimeProvider(FrozenNow);
        _linkService = Substitute.For<IGuardianLinkService>();
    }

    private static ConsumeContext<T> ContextFor<T>(T message) where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }

    [Test]
    public async Task Consume_GuardianAccessGranted_UpsertsLinkWithPermissions()
    {
        // Arrange
        var sut = new GuardianAccessGrantedConsumer(_linkService,
            Substitute.For<ILogger<GuardianAccessGrantedConsumer>>());
        var guardianId = Guid.NewGuid();
        var minorId = Guid.NewGuid();

        // Act
        await sut.Consume(ContextFor(new GuardianAccessGrantedEvent
        {
            Version = 1,
            HouseholdId = Guid.NewGuid(),
            GuardianId = guardianId,
            MinorId = minorId,
            Permissions = GuardianPermission.View | GuardianPermission.Message
        }));

        // Assert
        await _linkService.Received(1).UpsertAsync(guardianId, minorId,
            GuardianPermission.View | GuardianPermission.Message);
    }

    [Test]
    public async Task Consume_GuardianAccessRevoked_RemovesLink()
    {
        // Arrange
        var sut = new GuardianAccessRevokedConsumer(_linkService,
            Substitute.For<ILogger<GuardianAccessRevokedConsumer>>());
        var guardianId = Guid.NewGuid();
        var minorId = Guid.NewGuid();

        // Act
        await sut.Consume(ContextFor(new GuardianAccessRevokedEvent
        {
            Version = 1,
            HouseholdId = Guid.NewGuid(),
            GuardianId = guardianId,
            MinorId = minorId,
            RevokedAt = Now
        }));

        // Assert
        await _linkService.Received(1).RemoveAsync(guardianId, minorId);
    }

    [Test]
    public async Task Consume_MinorTransitionedToAdult_PurgesAllLinksForWard()
    {
        // Arrange
        var sut = new MinorTransitionedToAdultConsumer(_linkService,
            Substitute.For<ILogger<MinorTransitionedToAdultConsumer>>());
        var userId = Guid.NewGuid();

        // Act
        await sut.Consume(ContextFor(new MinorTransitionedToAdultEvent
        {
            UserId = userId,
            HouseholdId = Guid.NewGuid()
        }));

        // Assert
        await _linkService.Received(1).RemoveAllForWardAsync(userId);
    }
}
