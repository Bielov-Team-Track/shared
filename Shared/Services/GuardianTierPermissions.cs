using Shared.Enums;

namespace Shared.Services;

public static class GuardianTierPermissions
{
    /// <summary>The most a tier may ever hold. A write is rejected, never silently trimmed.</summary>
    public static GuardianPermission Ceiling(GuardianTier tier) => tier switch
    {
        GuardianTier.Guardian => GuardianPermission.View | GuardianPermission.RSVP
                               | GuardianPermission.Register | GuardianPermission.Message
                               | GuardianPermission.Pay | GuardianPermission.Admin,
        GuardianTier.Contact  => GuardianPermission.View,
        GuardianTier.Payer    => GuardianPermission.View | GuardianPermission.Pay,
        _ => GuardianPermission.None
    };

    public static bool IsWithinCeiling(GuardianTier tier, GuardianPermission permissions) =>
        (permissions & ~Ceiling(tier)) == GuardianPermission.None;
}
