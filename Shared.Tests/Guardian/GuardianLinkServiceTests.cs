using FluentAssertions;
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
public class GuardianLinkServiceTests
{
    private const string VerifiedKeyPrefix = "guardian-links-verified:";
    private const string ForcedKeyPrefix = "guardian-links-forced:";

    private static readonly Guid GuardianId = Guid.NewGuid();
    private static readonly Guid WardId = Guid.NewGuid();
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<string, byte[]> _cacheStore = new();

    private FakeTimeProvider _timeProvider = null!;
    private IRepository<GuardianLink> _linkRepository = null!;
    private IRepository<UserProfile> _userProfileRepository = null!;
    private IGuardianLinkSource _source = null!;
    private IDistributedCache _cache = null!;
    private IAgeTierService _ageTierService = null!;
    private GuardianLinkService _sut = null!;

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;
    private static string VerifiedKey => VerifiedKeyPrefix + GuardianId;
    private static string ForcedKey => ForcedKeyPrefix + GuardianId;

    [SetUp]
    public void SetUp()
    {
        _cacheStore.Clear();
        _timeProvider = new FakeTimeProvider(FrozenNow);
        _linkRepository = Substitute.For<IRepository<GuardianLink>>();
        _userProfileRepository = Substitute.For<IRepository<UserProfile>>();
        _source = Substitute.For<IGuardianLinkSource>();
        _cache = Substitute.For<IDistributedCache>();
        _ageTierService = new AgeTierService(_timeProvider);

        _linkRepository.Query().Returns(new List<GuardianLink>().BuildMock());
        _userProfileRepository.Query().Returns(new List<UserProfile>().BuildMock());
        StubCacheAsAStore();

        _sut = new GuardianLinkService(_linkRepository, _userProfileRepository, _source, _cache,
            _ageTierService, Substitute.For<ILogger<GuardianLinkService>>());
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

    private void GivenProfiles(params UserProfile[] profiles) =>
        _userProfileRepository.Query().Returns(profiles.ToList().BuildMock());

    private void GivenRemoteWards(params Guid[] wardIds) =>
        _source.GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)wardIds.ToList());

    private void GivenRemoteAccess(Guid wardId, GuardianLinkAccess? access) =>
        _source.CheckGuardianAccessAsync(GuardianId, wardId, Arg.Any<CancellationToken>()).Returns(access);

    private static GuardianLink Link(Guid guardianId, Guid wardId, GuardianPermission permissions) =>
        new() { GuardianUserId = guardianId, WardUserId = wardId, Permissions = permissions };

    [Test]
    public async Task UpsertAsync_NewPair_AddsLink()
    {
        // Arrange
        // Act
        await _sut.UpsertAsync(GuardianId, WardId, GuardianPermission.View | GuardianPermission.Message);

        // Assert
        _linkRepository.Received(1).Add(Arg.Is<GuardianLink>(l =>
            l.GuardianUserId == GuardianId && l.WardUserId == WardId &&
            l.Permissions.HasFlag(GuardianPermission.Message)));
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task UpsertAsync_ExistingPair_UpdatesPermissions()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.View);
        GivenLinks(existing);

        // Act
        await _sut.UpsertAsync(GuardianId, WardId, GuardianPermission.View | GuardianPermission.Message);

        // Assert
        existing.Permissions.Should().Be(GuardianPermission.View | GuardianPermission.Message);
        _linkRepository.Received(1).Update(existing);
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
    }

    [Test]
    public async Task UpsertAsync_PermissionsUnchanged_DoesNotSave()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId, GuardianPermission.Message));

        // Act
        await _sut.UpsertAsync(GuardianId, WardId, GuardianPermission.Message);

        // Assert
        _linkRepository.DidNotReceive().Update(Arg.Any<GuardianLink>());
        await _linkRepository.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task RemoveAsync_ExistingLink_DeletesAndSaves()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.Message);
        GivenLinks(existing);

        // Act
        await _sut.RemoveAsync(GuardianId, WardId);

        // Assert
        _linkRepository.Received(1).Delete(existing);
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task RemoveAsync_NoExistingLink_NoOp()
    {
        // Arrange
        // Act
        await _sut.RemoveAsync(GuardianId, WardId);

        // Assert
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
        await _linkRepository.DidNotReceive().SaveChangesAsync();
    }

    [Test]
    public async Task RemoveAllForWardAsync_MultipleGuardians_DeletesAllMatchingLinksOnce()
    {
        // Arrange
        var otherGuardian = Guid.NewGuid();
        var otherWard = Guid.NewGuid();
        var link1 = Link(GuardianId, WardId, GuardianPermission.Message);
        var link2 = Link(otherGuardian, WardId, GuardianPermission.View);
        var unrelated = Link(GuardianId, otherWard, GuardianPermission.Message);
        GivenLinks(link1, link2, unrelated);

        // Act
        await _sut.RemoveAllForWardAsync(WardId);

        // Assert
        _linkRepository.Received(1).Delete(link1);
        _linkRepository.Received(1).Delete(link2);
        _linkRepository.DidNotReceive().Delete(unrelated);
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task RemoveAllForUserAsync_UserIsGuardianOfOneAndWardOfAnother_DeletesBothRowsOnce()
    {
        // Arrange
        var otherGuardian = Guid.NewGuid();
        var asGuardianLink = Link(GuardianId, WardId, GuardianPermission.Message);
        var asWardLink = Link(otherGuardian, GuardianId, GuardianPermission.View);
        GivenLinks(asGuardianLink, asWardLink);

        // Act
        await _sut.RemoveAllForUserAsync(GuardianId);

        // Assert
        _linkRepository.Received(1).Delete(asGuardianLink);
        _linkRepository.Received(1).Delete(asWardLink);
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task GetMinorWardIdsAsync_FiltersRequiredBitAndMinors()
    {
        // Arrange
        var adultWard = Guid.NewGuid();
        var noBitWard = Guid.NewGuid();
        GivenLinks(
            Link(GuardianId, WardId, GuardianPermission.Message),
            Link(GuardianId, adultWard, GuardianPermission.Message),
            Link(GuardianId, noBitWard, GuardianPermission.View));
        GivenProfiles(
            new UserProfile { Id = WardId, DateOfBirth = Now.AddYears(-13) },
            new UserProfile { Id = adultWard, DateOfBirth = Now.AddYears(-20) },
            new UserProfile { Id = noBitWard, DateOfBirth = Now.AddYears(-13) });

        // Act
        var wards = await _sut.GetMinorWardIdsAsync(GuardianId, GuardianPermission.Message);

        // Assert
        wards.Should().Equal(WardId);
    }

    [Test]
    public async Task GetMinorWardIdsAsync_WardIsAnAdultByLocalDob_ExcludesWard()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId, GuardianPermission.Message));
        GivenProfiles(new UserProfile { Id = WardId, DateOfBirth = Now.AddYears(-25) });

        // Act
        var wards = await _sut.GetMinorWardIdsAsync(GuardianId, GuardianPermission.Message);

        // Assert
        wards.Should().BeEmpty();
    }

    [Test]
    public async Task GetMinorWardIdsAsync_WardHasNullDateOfBirth_ExcludesWard()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId, GuardianPermission.Message));
        GivenProfiles(new UserProfile { Id = WardId, DateOfBirth = null });

        // Act
        var wards = await _sut.GetMinorWardIdsAsync(GuardianId, GuardianPermission.Message);

        // Assert
        wards.Should().BeEmpty();
    }

    [Test]
    public async Task GetWardIdsAsync_WardHasNullDateOfBirth_StillReturnsWard()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId, GuardianPermission.View));
        GivenProfiles(new UserProfile { Id = WardId, DateOfBirth = null });

        // Act
        var wards = await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        wards.Should().Equal(WardId);
    }

    [Test]
    public async Task GetWardIdsAsync_WardIsAnAdultByLocalDob_StillReturnsWard()
    {
        // Arrange
        GivenLinks(Link(GuardianId, WardId, GuardianPermission.View));
        GivenProfiles(new UserProfile { Id = WardId, DateOfBirth = Now.AddYears(-30) });

        // Act
        var wards = await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        wards.Should().Equal(WardId);
    }

    [Test]
    public async Task GetWardIdsAsync_PermissionBitMissing_ExcludesWard()
    {
        // Arrange
        var messagingWard = Guid.NewGuid();
        GivenLinks(
            Link(GuardianId, WardId, GuardianPermission.View),
            Link(GuardianId, messagingWard, GuardianPermission.View | GuardianPermission.Message));

        // Act
        var wards = await _sut.GetWardIdsAsync(GuardianId, GuardianPermission.Message);

        // Assert
        wards.Should().Equal(messagingWard);
    }

    [Test]
    public async Task GetWardIdsAsync_OtherGuardiansLink_IsNotReturned()
    {
        // Arrange
        var otherGuardian = Guid.NewGuid();
        GivenLinks(Link(otherGuardian, WardId, GuardianPermission.View));

        // Act
        var wards = await _sut.GetWardIdsAsync(GuardianId);

        // Assert
        wards.Should().BeEmpty();
    }

    [Test]
    public async Task EnsureFreshAsync_MarkerPresentAndNotForced_SkipsSource()
    {
        // Arrange
        _cacheStore[VerifiedKey] = "1"u8.ToArray();

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        await _source.DidNotReceive().GetMinorsForGuardianAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureFreshAsync_MarkerPresentAndForced_ReconcilesAnyway()
    {
        // Arrange
        _cacheStore[VerifiedKey] = "1"u8.ToArray();
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.Message));

        // Act
        await _sut.EnsureFreshAsync(GuardianId, force: true);

        // Assert
        await _source.Received(1).GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>());
        _linkRepository.Received(1).Add(Arg.Is<GuardianLink>(l => l.WardUserId == WardId));
    }

    [Test]
    public async Task EnsureFreshAsync_ForcedTwiceWithin60s_ReconcilesOnce()
    {
        // Arrange
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.Message));

        // Act
        await _sut.EnsureFreshAsync(GuardianId, force: true);
        await _sut.EnsureFreshAsync(GuardianId, force: true);

        // Assert
        await _source.Received(1).GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureFreshAsync_ForcedAfterFailure_SetsBothMarkers()
    {
        // Arrange
        _source.GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<Guid>>>(_ => throw new Exception("profiles down"));

        // Act
        await _sut.EnsureFreshAsync(GuardianId, force: true);

        // Assert
        await _cache.Received(1).SetAsync(VerifiedKey, Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
            Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(ForcedKey, Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromSeconds(60)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureFreshAsync_ReconcilesBothDirections()
    {
        // Arrange
        var staleWard = Guid.NewGuid();
        var stale = Link(GuardianId, staleWard, GuardianPermission.Message);
        GivenLinks(stale);
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.Message));

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        _linkRepository.Received(1).Add(Arg.Is<GuardianLink>(l => l.WardUserId == WardId));
        _linkRepository.Received(1).Delete(stale);
        await _linkRepository.Received(1).SaveChangesAsync();
        await _cache.Received(1).SetAsync(VerifiedKey, Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromHours(1)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureFreshAsync_ExistingLinkPermissionsChanged_UpdatesInPlaceWithoutAdd()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.View);
        GivenLinks(existing);
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.View | GuardianPermission.Message));

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        existing.Permissions.Should().Be(GuardianPermission.View | GuardianPermission.Message);
        _linkRepository.Received(1).Update(existing);
        _linkRepository.DidNotReceive().Add(Arg.Any<GuardianLink>());
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task EnsureFreshAsync_SourceFailure_DegradesWithoutThrowing()
    {
        // Arrange
        _source.GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<Guid>>>(_ => throw new Exception("profiles down"));

        // Act
        var act = async () => await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        await act.Should().NotThrowAsync();
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
    }

    [Test]
    public async Task EnsureFreshAsync_SaveChangesThrows_DegradesWithoutThrowingAndSetsFailureMarker()
    {
        // Arrange
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.Message));
        _linkRepository.SaveChangesAsync().Returns<Task>(_ => throw new Exception("db down"));

        // Act
        var act = async () => await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        await act.Should().NotThrowAsync();
        await _cache.Received(1).SetAsync(VerifiedKey, Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureFreshAsync_MarkerCacheReadThrows_ReconcilesAnywayWithoutThrowing()
    {
        // Arrange
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]?>>(_ => throw new Exception("redis down"));
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Exception("redis down"));
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.Message));

        // Act
        var act = async () => await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        await act.Should().NotThrowAsync();
        await _source.Received(1).GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureFreshAsync_WardListedButAccessDenied_DeletesLocalLink()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.Message);
        GivenLinks(existing);
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(false, GuardianPermission.None));

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        _linkRepository.Received(1).Delete(existing);
        await _linkRepository.Received(1).SaveChangesAsync();
    }

    [Test]
    public async Task EnsureFreshAsync_WardListedButAccessCheckFails_RetainsLocalLink()
    {
        // Arrange
        var existing = Link(GuardianId, WardId, GuardianPermission.Message);
        GivenLinks(existing);
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, null);

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        _linkRepository.DidNotReceive().Delete(Arg.Any<GuardianLink>());
    }

    [Test]
    public async Task EnsureFreshAsync_DuplicateWardIdInRemoteMinors_AddsOnce()
    {
        // Arrange
        GivenRemoteWards(WardId, WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.Message));

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        _linkRepository.Received(1).Add(Arg.Is<GuardianLink>(l => l.WardUserId == WardId));
    }

    [Test]
    public async Task UpsertAsync_NewPairWithATier_WritesTheTier()
    {
        // Arrange
        // Act
        await _sut.UpsertAsync(GuardianId, WardId, GuardianPermission.View, GuardianTier.Contact);

        // Assert
        _linkRepository.Received(1).Add(Arg.Is<GuardianLink>(l => l.Tier == GuardianTier.Contact));
    }

    [Test]
    public async Task UpsertAsync_TierChangedPermissionsUnchanged_UpdatesTheRow()
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

        // Act
        await _sut.UpsertAsync(GuardianId, WardId, GuardianPermission.View, GuardianTier.Guardian);

        // Assert
        existing.Tier.Should().Be(GuardianTier.Guardian);
        _linkRepository.Received(1).Update(existing);
    }

    [Test]
    public async Task EnsureFreshAsync_NewWard_CopiesTheRemoteTier()
    {
        // Arrange
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.View, GuardianTier.Payer));

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        _linkRepository.Received(1).Add(Arg.Is<GuardianLink>(l => l.Tier == GuardianTier.Payer));
    }

    [Test]
    public async Task EnsureFreshAsync_RemoteTierDiffersFromLocal_UpdatesTheTier()
    {
        // Arrange
        var existing = new GuardianLink
        {
            GuardianUserId = GuardianId,
            WardUserId = WardId,
            Permissions = GuardianPermission.View,
            Tier = GuardianTier.Guardian
        };
        GivenLinks(existing);
        GivenRemoteWards(WardId);
        GivenRemoteAccess(WardId, new GuardianLinkAccess(true, GuardianPermission.View, GuardianTier.Contact));

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        existing.Tier.Should().Be(GuardianTier.Contact);
        _linkRepository.Received(1).Update(existing);
    }
}
