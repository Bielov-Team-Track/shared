using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Shared.DataAccess;
using Shared.Models;

namespace Shared.Tests.Persistence;

// Composite-key entity that intentionally does NOT inherit BaseEntity.
// 'Name' exists so we can mutate a non-PK column in update-path tests.
public class CompositeAuditable : IAuditable
{
    public Guid PartA { get; set; }
    public Guid PartB { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TestDbContext : BaseDbContext
{
    public TestDbContext(DbContextOptions options) : base(options) { }
    public TestDbContext(DbContextOptions options, TimeProvider timeProvider) : base(options, timeProvider) { }

    public DbSet<CompositeAuditable> CompositeAuditables => Set<CompositeAuditable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompositeAuditable>().HasKey(x => new { x.PartA, x.PartB });
    }
}

[TestFixture]
[Category("Unit")]
public class AuditInterceptorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);

    private static DbContextOptions BuildOptions(string dbName) =>
        new DbContextOptionsBuilder().UseInMemoryDatabase(dbName).Options;

    [Test]
    public async Task Interceptor_populates_CreatedAt_and_UpdatedAt_on_composite_key_IAuditable_entity_when_added()
    {
        var clock = new FakeTimeProvider(T0);
        await using var ctx = new TestDbContext(
            BuildOptions(nameof(Interceptor_populates_CreatedAt_and_UpdatedAt_on_composite_key_IAuditable_entity_when_added)),
            clock);

        var entity = new CompositeAuditable
        {
            PartA = Guid.NewGuid(),
            PartB = Guid.NewGuid(),
            Name = "initial"
        };
        ctx.Add(entity);
        await ctx.SaveChangesAsync();

        Assert.That(entity.CreatedAt, Is.EqualTo(T0.UtcDateTime));
        Assert.That(entity.UpdatedAt, Is.EqualTo(T0.UtcDateTime));
    }

    [Test]
    public async Task Interceptor_preserves_CreatedAt_but_advances_UpdatedAt_on_non_key_modification()
    {
        var clock = new FakeTimeProvider(T0);
        await using var ctx = new TestDbContext(
            BuildOptions(nameof(Interceptor_preserves_CreatedAt_but_advances_UpdatedAt_on_non_key_modification)),
            clock);

        var entity = new CompositeAuditable
        {
            PartA = Guid.NewGuid(),
            PartB = Guid.NewGuid(),
            Name = "initial"
        };
        ctx.Add(entity);
        await ctx.SaveChangesAsync();
        var originalCreated = entity.CreatedAt;

        clock.Advance(TimeSpan.FromMinutes(5));
        entity.Name = "renamed";              // non-key mutation
        await ctx.SaveChangesAsync();

        Assert.That(entity.CreatedAt, Is.EqualTo(originalCreated));
        Assert.That(entity.UpdatedAt, Is.EqualTo(T0.AddMinutes(5).UtcDateTime));
    }
}
