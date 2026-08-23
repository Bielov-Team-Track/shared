using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Shared.Guardian.Data;
using Shared.Guardian.Models;

namespace Shared.Tests.Guardian;

[TestFixture]
[Category("Unit")]
public class GuardianLinkConfigurationTests
{
    /// <summary>
    /// GuardianLinkService reads this name off the PostgresException to tell a lost insert race
    /// apart from a real failure. If the model ever emits a different name the service silently
    /// falls back to the broad catch, so pin the two together here.
    /// </summary>
    [Test]
    public void GuardianWardIndex_IsNamedForTheConstantTheRaceHandlerMatches()
    {
        // Arrange
        using var context = new GuardianModelContext(
            new DbContextOptionsBuilder<GuardianModelContext>()
                .UseNpgsql("Host=localhost;Database=model-only").Options);

        // Act
        var index = context.Model.FindEntityType(typeof(GuardianLink))!.GetIndexes().Single(i => i.IsUnique);

        // Assert
        index.GetDatabaseName().Should().Be(GuardianLinkSchema.GuardianWardUniqueIndex);
    }

    private sealed class GuardianModelContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfiguration(new GuardianLinkConfiguration());
    }
}
