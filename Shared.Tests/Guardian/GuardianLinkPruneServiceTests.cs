using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using MockQueryable;
using NSubstitute;
using NSubstitute.Core;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Guardian.Hosting;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Models;

namespace Shared.Tests.Guardian;

[TestFixture]
[Category("Unit")]
public class GuardianLinkPruneServiceTests
{
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan AdvanceStep = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private readonly List<(Guid GuardianId, bool Force)> _reconciles = new();

    private FakeTimeProvider _timeProvider = null!;
    private IRepository<GuardianLink> _linkRepository = null!;
    private IGuardianLinkService _linkService = null!;
    private int _sweeps;
    private GuardianLinkPruneService _sut = null!;

    private int ReconcileCount
    {
        get { lock (_reconciles) return _reconciles.Count; }
    }

    [SetUp]
    public void SetUp()
    {
        lock (_reconciles) _reconciles.Clear();
        _sweeps = 0;
        _timeProvider = new FakeTimeProvider(FrozenNow);
        _linkRepository = Substitute.For<IRepository<GuardianLink>>();
        _linkService = Substitute.For<IGuardianLinkService>();
        _linkService.EnsureFreshAsync(Arg.Any<Guid>(), Arg.Any<bool>()).Returns(Record);
        GivenGuardiansWithLinks();

        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IRepository<GuardianLink>)).Returns(_linkRepository);
        provider.GetService(typeof(IGuardianLinkService)).Returns(_linkService);
        scope.ServiceProvider.Returns(provider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _sut = new GuardianLinkPruneService(scopeFactory,
            Substitute.For<ILogger<GuardianLinkPruneService>>(), _timeProvider);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _sut.StopAsync(CancellationToken.None);
        _sut.Dispose();
    }

    private Task Record(CallInfo call)
    {
        lock (_reconciles) _reconciles.Add((call.ArgAt<Guid>(0), call.ArgAt<bool>(1)));
        return Task.CompletedTask;
    }

    private void GivenGuardiansWithLinks(params Guid[] guardianIds)
    {
        var links = guardianIds
            .Select(id => new GuardianLink
            {
                GuardianUserId = id,
                WardUserId = Guid.NewGuid(),
                Permissions = GuardianPermission.View
            })
            .ToList();
        _linkRepository.QueryNoTracking().Returns(_ =>
        {
            Interlocked.Increment(ref _sweeps);
            return links.BuildMock();
        });
        // Arranging the stub invokes the previous one, which already counted a sweep.
        Volatile.Write(ref _sweeps, 0);
    }

    private async Task AdvanceUntilAsync(Func<bool> condition)
    {
        var deadline = TimeProvider.System.GetUtcNow() + WaitTimeout;
        while (!condition())
        {
            if (TimeProvider.System.GetUtcNow() > deadline)
                Assert.Fail("Timed out waiting for the prune sweep");
            _timeProvider.Advance(AdvanceStep);
            await Task.Delay(PollDelay);
        }
    }

    [Test]
    public async Task ExecuteAsync_AfterStartupDelay_ForcesReconcileForEveryGuardianWithALink()
    {
        // Arrange
        var firstGuardian = Guid.NewGuid();
        var secondGuardian = Guid.NewGuid();
        GivenGuardiansWithLinks(firstGuardian, secondGuardian, firstGuardian);

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(() => ReconcileCount >= 2);

        // Assert
        List<(Guid GuardianId, bool Force)> reconciles;
        lock (_reconciles) reconciles = _reconciles.ToList();
        reconciles.Should().BeEquivalentTo(new[]
        {
            (GuardianId: firstGuardian, Force: true),
            (GuardianId: secondGuardian, Force: true)
        });
    }

    [Test]
    public async Task ExecuteAsync_AfterTheInterval_RunsAgain()
    {
        // Arrange
        var guardianId = Guid.NewGuid();
        GivenGuardiansWithLinks(guardianId);

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(() => ReconcileCount >= 1);
        await AdvanceUntilAsync(() => ReconcileCount >= 2);

        // Assert
        await _linkService.Received(2).EnsureFreshAsync(guardianId, true);
    }

    [Test]
    public async Task ExecuteAsync_OneGuardianThrows_ContinuesWithTheRest()
    {
        // Arrange
        var failingGuardian = Guid.NewGuid();
        var healthyGuardian = Guid.NewGuid();
        GivenGuardiansWithLinks(failingGuardian, healthyGuardian);
        _linkService.EnsureFreshAsync(failingGuardian, Arg.Any<bool>()).Returns<Task>(call =>
        {
            Record(call);
            throw new InvalidOperationException("profiles down");
        });

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(() => ReconcileCount >= 2);

        // Assert
        await _linkService.Received(1).EnsureFreshAsync(healthyGuardian, true);
    }

    [Test]
    public async Task ExecuteAsync_NoLinks_DoesNothingAndDoesNotThrow()
    {
        // Arrange
        GivenGuardiansWithLinks();

        // Act
        await _sut.StartAsync(CancellationToken.None);
        await AdvanceUntilAsync(() => Volatile.Read(ref _sweeps) >= 1);

        // Assert
        await _linkService.DidNotReceive().EnsureFreshAsync(Arg.Any<Guid>(), Arg.Any<bool>());
    }
}
