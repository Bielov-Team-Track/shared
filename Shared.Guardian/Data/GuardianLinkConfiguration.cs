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
        builder.HasIndex(l => new { l.GuardianUserId, l.WardUserId }).IsUnique();
        builder.HasIndex(l => l.WardUserId);
    }
}
