using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Models;
using Shared.Guardian.Services;
using Shared.Models;
using Shared.Services.Services;
using Shared.Services.Services.Interfaces;

namespace Shared.Tests.Guardian;

[TestFixture]
[Category("Unit")]
public class GuardianLinkServiceWardTests
{
    private static readonly Guid WardId = Guid.NewGuid();
    private static readonly Guid GuardianId = Guid.NewGuid();
    private static readonly Guid OtherGuardianId = Guid.NewGuid();
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<string, byte[]> _cacheStore = new();

    private IRepository<GuardianLink> _linkRepository = null!;
    private IGuardianLinkSource _source = null!;
    private IDistributedCache _cache = null!;
    private RecordingLogger<GuardianLinkService> _logger = null!;
    private GuardianLinkService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _cacheStore.Clear();
        _linkRepository = Substitute.For<IRepository<GuardianLink>>();
        _source = Substitute.For<IGuardianLinkSource>();
        _cache = Substitute.For<IDistributedCache>();
        _logger = new RecordingLogger<GuardianLinkService>();

        var userProfileRepository = Substitute.For<IRepository<UserProfile>>();
        userProfileRepository.Query().Returns(new List<UserProfile>().BuildMock());

        GivenLinks();
        GivenRemoteGuardians();
        StubCacheAsAStore();

        _sut = new GuardianLinkService(_linkRepository, userProfileRepository, _source, _cache,
            new AgeTierService(new FakeTimeProvider(FrozenNow)), _logger);
    }

    private void StubCacheAsAStore()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(
                _cacheStore.TryGetValue(ci.ArgAt<string>(0), out var value) ? value : null));
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _cacheStore[ci.ArgAt<string>(0)] = ci.ArgAt<byte[]>(1);
                return Task.CompletedTask;
            });
    }

    private void GivenLinks(params GuardianLink[] links) =>
        _linkRepository.Query().Returns(links.ToList().BuildMock());

    private void GivenRemoteGuardians(params Guid[] guardianIds) =>
        _source.GetGuardiansForMinorAsync(WardId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)guardianIds.ToList());

    private static GuardianLink Link(Guid guardianId, Guid wardId, GuardianPermission permissions) =>
        new() { GuardianUserId = guardianId, WardUserId = wardId, Permissions = permissions };

    private GuardianLink AddedLink() => _linkRepository.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == nameof(IRepository<GuardianLink>.Add))
        .Select(c => (GuardianLink)c.GetArguments()[0]!)
        .Single();

    [Test]
    public async Task GetGuardianIdsForWardAsync_TwoGuardians_ReturnsBoth()
    {
        // Arrange
        GivenLinks(
            Link(GuardianId, WardId, GuardianPermission.View),
            Link(OtherGuardianId, WardId, GuardianPermission.Pay),
            Link(GuardianId, Guid.NewGuid(), GuardianPermission.View));

        // Act
        var result = await _sut.GetGuardianIdsForWardAsync(WardId);

        // Assert
        result.Should().BeEquivalentTo(new[] { GuardianId, OtherGuardianId });
    }

    [Test]
    public async Task GetGuardianIdsForWardAsync_NoLinks_ReturnsEmpty()
    {
        // Arrange
        // Act
        var result = await _sut.GetGuardianIdsForWardAsync(WardId);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task EnsureWardFreshAsync_MarkerWarmAndNotForced_SkipsTheSource()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);
        await _sut.EnsureWardFreshAsync(WardId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        await _source.Received(1).GetGuardiansForMinorAsync(WardId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureWardFreshAsync_Forced_ReconcilesAnyway()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);
        await _sut.EnsureWardFreshAsync(WardId);
        _cacheStore.Remove("guardian-links-ward-forced:" + WardId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId, force: true);

        // Assert
        await _source.Received(2).GetGuardiansForMinorAsync(WardId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureWardFreshAsync_ForcedTwiceWithin60s_ReconcilesOnce()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId, force: true);
        await _sut.EnsureWardFreshAsync(WardId, force: true);

        // Assert
        await _source.Received(1).GetGuardiansForMinorAsync(WardId, Arg.Any<CancellationToken>());
    }

    /// <summary>A people list degrades to "shows what we know"; it does not 500.</summary>
    [Test]
    public async Task EnsureWardFreshAsync_SourceThrows_DoesNotThrowAndLeavesLinksAlone()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.Pay);
        GivenLinks(existing);
        _source.GetGuardiansForMinorAsync(WardId, Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Guid>>(_ => throw new InvalidOperationException("profiles unavailable"));

        // Act
        var act = () => _sut.EnsureWardFreshAsync(WardId);

        // Assert
        await act.Should().NotThrowAsync();
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
        await _linkRepository.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task EnsureWardFreshAsync_NewGuardian_UpsertsWithViewOnly()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        AddedLink().Should().Match<GuardianLink>(l =>
            l.GuardianUserId == GuardianId &&
            l.WardUserId == WardId &&
            l.Permissions == GuardianPermission.View);
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    /// <summary>
    /// The one that matters. GetGuardiansForMinor carries ids only, so a reconcile that wrote View
    /// over an existing row would silently strip a Pay grant every time a people list rendered.
    /// </summary>
    [Test]
    public async Task EnsureWardFreshAsync_ExistingGuardian_DoesNotDowngradePermissions()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.View | GuardianPermission.Pay);
        GivenLinks(existing);
        GivenRemoteGuardians(GuardianId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        existing.Permissions.Should().Be(GuardianPermission.View | GuardianPermission.Pay);
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Update(Arg.Any<GuardianLink>());
    }

    [Test]
    public async Task EnsureWardFreshAsync_GuardianAbsentFromTheSource_RemovesTheLocalLink()
    {
        // Arrange
        var stale = Link(OtherGuardianId, WardId, GuardianPermission.View);
        GivenLinks(Link(GuardianId, WardId, GuardianPermission.View), stale);
        GivenRemoteGuardians(GuardianId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        _linkRepository.Received(1).Delete(stale);
    }

    /// <summary>
    /// A ward-seeded row buys visibility and nothing else — it must never satisfy a permission
    /// gate that only a real grant event can answer.
    /// </summary>
    [Test]
    public async Task EnsureWardFreshAsync_SeededLink_DoesNotSatisfyGetWardIdsAsyncPay()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);
        await _sut.EnsureWardFreshAsync(WardId);
        GivenLinks(AddedLink());

        // Act
        var payWards = await _sut.GetWardIdsAsync(GuardianId, GuardianPermission.Pay);
        var anyWards = await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        payWards.Should().BeEmpty();
        anyWards.Should().Equal(WardId);
    }

    [Test]
    public async Task EnsureWardFreshAsync_NewGuardian_SeedsTheGuardianTier()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        AddedLink().Tier.Should().Be(GuardianTier.Guardian);
    }

    /// <summary>
    /// The tier's half of EnsureWardFreshAsync_ExistingGuardian_DoesNotDowngradePermissions:
    /// GetGuardiansForMinor carries no tier either, so a reconcile that wrote one would promote
    /// every Contact to a full guardian each time a people list rendered.
    /// </summary>
    [Test]
    public async Task EnsureWardFreshAsync_ExistingGuardian_LeavesTheTierAlone()
    {
        // Arrange
        var existing = new GuardianLink
        {
            GuardianUserId = GuardianId,
            WardUserId = WardId,
            Permissions = GuardianPermission.View,
            Tier = GuardianTier.Contact
        };
        GivenLinks(existing);
        GivenRemoteGuardians(GuardianId);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        existing.Tier.Should().Be(GuardianTier.Contact);
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Update(Arg.Any<GuardianLink>());
    }

    /// <summary>
    /// Two people lists rendering the same ward at once both seed the same guardian row; the loser
    /// gets 23505. The ward is linked either way, so this is not the source being down.
    /// </summary>
    [Test]
    public async Task EnsureWardFreshAsync_LosesTheInsertRace_IsNotReportedAsProfilesUnavailable()
    {
        // Arrange
        GivenRemoteGuardians(GuardianId);
        _linkRepository.SaveChangesAsync().Returns<Task>(_ => throw GuardianLinkFailures.DuplicatePair());

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        _logger.Entries.Should().NotContain(e => e.Message.Contains("profiles unavailable"));
        _logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning);
        _linkRepository.Received(1).Detach(Arg.Is<GuardianLink>(l => l.GuardianUserId == GuardianId));
    }

    [Test]
    public async Task EnsureWardFreshAsync_SaveFailsForAnotherReason_NamesTheActualFailureAndDetaches()
    {
        // Arrange
        var failure = new DbUpdateException("deadlock detected");
        GivenRemoteGuardians(GuardianId);
        _linkRepository.SaveChangesAsync().Returns<Task>(_ => throw failure);

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        var warning = _logger.Entries.Single(e => e.Level == LogLevel.Warning);
        warning.Message.Should().Contain(nameof(DbUpdateException));
        warning.Message.Should().NotContain("profiles unavailable");
        _linkRepository.Received(1).Detach(Arg.Is<GuardianLink>(l => l.GuardianUserId == GuardianId));
    }
}
