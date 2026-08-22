using Microsoft.EntityFrameworkCore;

namespace Shared.Guardian.Data;

public static class GuardianModelBuilderExtensions
{
    /// <summary>
    /// Each service scans only its own assembly for configurations, so this one has to be
    /// applied by hand.
    /// </summary>
    public static ModelBuilder ApplyGuardianLinkConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new GuardianLinkConfiguration());
        return modelBuilder;
    }
}
