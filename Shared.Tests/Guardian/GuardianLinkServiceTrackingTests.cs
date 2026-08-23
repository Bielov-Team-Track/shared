using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Time.Testing;
using MockQueryable;
using NSubstitute;
using Shared.DataAccess.Repositories;
using Shared.DataAccess.Repositories.Interfaces;
using Shared.Enums;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Models;
using Shared.Guardian.Services;
using Shared.Models;
using Shared.Services.Services;

namespace Shared.Tests.Guardian;

/// <summary>
/// The reconcile runs on the REQUEST's scoped DbContext, so a write it stages and fails to save
/// is still tracked when the request's own SaveChangesAsync runs later. These drive a real
/// ChangeTracker rather than a repository substitute because that leak is the defect.
/// </summary>
[TestFixture]
[Category("Unit")]
public class GuardianLinkServiceTrackingTests
{
    private static readonly Guid GuardianId = Guid.NewGuid();
    private static readonly Guid WardId = Guid.NewGuid();
    private static readonly Guid StaleWardId = Guid.NewGuid();
    private static readonly Guid ExistingWardId = Guid.NewGuid();
    private static readonly DateTimeOffset FrozenNow = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private FlakySaveDbContext _context = null!;
    private IGuardianLinkSource _source = null!;
    private RecordingLogger<GuardianLinkService> _logger = null!;
    private GuardianLinkService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _context = new FlakySaveDbContext(new DbContextOptionsBuilder<FlakySaveDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _source = Substitute.For<IGuardianLinkSource>();
        _logger = new RecordingLogger<GuardianLinkService>();

        var userProfileRepository = Substitute.For<IRepository<UserProfile>>();
        userProfileRepository.Query().Returns(new List<UserProfile>().BuildMock());

        // Substitute.For<IDistributedCache>() answers GetAsync with an EMPTY ARRAY, not null,
        // which reads as a warm marker and skips the reconcile entirely.
        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        _sut = new GuardianLinkService(new BaseRepository<GuardianLink>(_context), userProfileRepository,
            _source, cache, new AgeTierService(new FakeTimeProvider(FrozenNow)), _logger);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private void Seed(params GuardianLink[] links)
    {
        _context.Set<GuardianLink>().AddRange(links);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private static GuardianLink Link(Guid guardianId, Guid wardId, GuardianPermission permissions) =>
        new() { GuardianUserId = guardianId, WardUserId = wardId, Permissions = permissions };

    private IReadOnlyList<EntityEntry> PendingWrites() => _context.ChangeTracker.Entries()
        .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
        .ToList();

    [Test]
    public async Task EnsureFreshAsync_LosesTheInsertRace_LeavesNothingStagedForTheNextSave()
    {
        // Arrange
        Seed(Link(GuardianId, StaleWardId, GuardianPermission.View),
            Link(GuardianId, ExistingWardId, GuardianPermission.View));
        _source.GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new List<Guid> { WardId, ExistingWardId });
        _source.CheckGuardianAccessAsync(GuardianId, WardId, Arg.Any<CancellationToken>())
            .Returns(new GuardianLinkAccess(true, GuardianPermission.Message));
        _source.CheckGuardianAccessAsync(GuardianId, ExistingWardId, Arg.Any<CancellationToken>())
            .Returns(new GuardianLinkAccess(true, GuardianPermission.Pay));
        _context.NextSaveFailure = GuardianLinkFailures.DuplicatePair();

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        PendingWrites().Should().BeEmpty();
        await _context.SaveChangesAsync();
        var rows = await _context.Set<GuardianLink>().AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().NotContain(l => l.WardUserId == WardId);
        rows.Should().Contain(l => l.WardUserId == StaleWardId);
        rows.Single(l => l.WardUserId == ExistingWardId).Permissions.Should().Be(GuardianPermission.View);
    }

    [Test]
    public async Task EnsureWardFreshAsync_LosesTheInsertRace_LeavesNothingStagedForTheNextSave()
    {
        // Arrange
        _source.GetGuardiansForMinorAsync(WardId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new List<Guid> { GuardianId });
        _context.NextSaveFailure = GuardianLinkFailures.DuplicatePair();

        // Act
        await _sut.EnsureWardFreshAsync(WardId);

        // Assert
        PendingWrites().Should().BeEmpty();
        await _context.SaveChangesAsync();
        (await _context.Set<GuardianLink>().AsNoTracking().CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// The broad catch is the one that fired in production. It has to drop the staged write too —
    /// a reconcile that could not save must never ride along on someone else's SaveChangesAsync.
    /// </summary>
    [Test]
    public async Task EnsureFreshAsync_SaveFailsForAnotherReason_LeavesNothingStagedForTheNextSave()
    {
        // Arrange
        _source.GetMinorsForGuardianAsync(GuardianId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new List<Guid> { WardId });
        _source.CheckGuardianAccessAsync(GuardianId, WardId, Arg.Any<CancellationToken>())
            .Returns(new GuardianLinkAccess(true, GuardianPermission.Message));
        _context.NextSaveFailure = new DbUpdateException("deadlock detected");

        // Act
        await _sut.EnsureFreshAsync(GuardianId);

        // Assert
        PendingWrites().Should().BeEmpty();
        await _context.SaveChangesAsync();
        (await _context.Set<GuardianLink>().AsNoTracking().CountAsync()).Should().Be(0);
    }

    private sealed class FlakySaveDbContext(DbContextOptions options) : DbContext(options)
    {
        public Exception? NextSaveFailure { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (NextSaveFailure is null) return base.SaveChangesAsync(cancellationToken);
            var failure = NextSaveFailure;
            NextSaveFailure = null;
            throw failure;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<GuardianLink>();
    }
}
