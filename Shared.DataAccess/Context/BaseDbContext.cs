using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.DataAccess;

public abstract class BaseDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;

    // Existing derived contexts call this single-arg constructor. Default to TimeProvider.System.
    protected BaseDbContext(DbContextOptions options)
        : this(options, TimeProvider.System) { }

    // Derived contexts that want a DI-supplied TimeProvider add a (DbContextOptions<T>, TimeProvider)
    // constructor and forward to this one.
    protected BaseDbContext(DbContextOptions options, TimeProvider timeProvider) : base(options)
    {
        _timeProvider = timeProvider;
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Authoritative: always overwrite on insert so any caller-supplied value is ignored.
                    // This is the single source of truth for audit timestamps; callers must not attempt
                    // to set them manually. (If you need a specific CreatedAt for seed/test data, insert
                    // via raw SQL or advance the TimeProvider instead.)
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
