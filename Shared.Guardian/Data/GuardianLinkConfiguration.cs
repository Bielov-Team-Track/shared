using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Guardian.Models;

namespace Shared.Guardian.Data;

public class GuardianLinkConfiguration : IEntityTypeConfiguration<GuardianLink>
{
    public void Configure(EntityTypeBuilder<GuardianLink> builder)
    {
        // Pinned rather than derived from a DbSet name: messages-service already owns this
        // table, so a service adopting the replica must land on exactly the same name.
        builder.ToTable("GuardianLinks");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.GuardianUserId).IsRequired();
        builder.Property(l => l.WardUserId).IsRequired();
        // int column on purpose: [Flags] enum queried with SQL bitwise AND
        builder.Property(l => l.Permissions).IsRequired();
        // int for the same reason as the column beside it: a service adopting the replica must
        // land on the same column type, and 0 reads back as Guardian for every pre-tier row.
        builder.Property(l => l.Tier).HasConversion<int>().IsRequired();
        builder.HasIndex(l => new { l.GuardianUserId, l.WardUserId }).IsUnique();
        builder.HasIndex(l => l.WardUserId);
    }
}
