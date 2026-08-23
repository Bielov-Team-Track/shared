using FluentAssertions;
using Shared.Enums;
using Shared.Services;

namespace Shared.Tests.Guardian;

[TestFixture]
[Category("Unit")]
public class GuardianTierPermissionsTests
{
    [Test]
    public void Ceiling_Guardian_IsEveryBit()
    {
        // Arrange
        var everyBit = GuardianPermission.View | GuardianPermission.RSVP
                     | GuardianPermission.Register | GuardianPermission.Message
                     | GuardianPermission.Pay | GuardianPermission.Admin;

        // Act
        var ceiling = GuardianTierPermissions.Ceiling(GuardianTier.Guardian);

        // Assert
        ceiling.Should().Be(everyBit);
    }

    [Test]
    public void Ceiling_Contact_IsViewOnly()
    {
        // Act
        var ceiling = GuardianTierPermissions.Ceiling(GuardianTier.Contact);

        // Assert
        ceiling.Should().Be(GuardianPermission.View);
    }

    [Test]
    public void Ceiling_Payer_IsViewAndPay()
    {
        // Act
        var ceiling = GuardianTierPermissions.Ceiling(GuardianTier.Payer);

        // Assert
        ceiling.Should().Be(GuardianPermission.View | GuardianPermission.Pay);
    }

    [Test]
    public void IsWithinCeiling_ContactWithMessage_IsFalse()
    {
        // Arrange
        var requested = GuardianPermission.View | GuardianPermission.Message;

        // Act
        var within = GuardianTierPermissions.IsWithinCeiling(GuardianTier.Contact, requested);

        // Assert
        within.Should().BeFalse("a Contact may be called and may read the schedule, never act or read messages");
    }

    [Test]
    public void IsWithinCeiling_GuardianWithEverything_IsTrue()
    {
        // Arrange
        var requested = GuardianPermission.View | GuardianPermission.RSVP
                      | GuardianPermission.Register | GuardianPermission.Message
                      | GuardianPermission.Pay | GuardianPermission.Admin;

        // Act
        var within = GuardianTierPermissions.IsWithinCeiling(GuardianTier.Guardian, requested);

        // Assert
        within.Should().BeTrue();
    }

    [Test]
    public void Ceiling_EveryDeclaredTier_IsCovered()
    {
        // Act & Assert
        foreach (var tier in Enum.GetValues<GuardianTier>())
            GuardianTierPermissions.Ceiling(tier).Should().NotBe(
                GuardianPermission.None,
                $"GuardianTier.{tier} has no ceiling in GuardianTierPermissions and would lock its " +
                "holders out — add an arm rather than letting it fall to the default");
    }
}
