namespace Shared.Guardian.Data;

public static class GuardianLinkSchema
{
    public const string TableName = "GuardianLinks";

    /// <summary>
    /// Pinned rather than left to EF's naming convention. GuardianLinkService recognises a
    /// concurrent reconcile of the same guardian by this exact name arriving on a unique
    /// violation, so a silent rename would turn that race back into a 500. Both migrations
    /// that already created the index (messages-service, clubs-service) used this name.
    /// </summary>
    public const string GuardianWardUniqueIndex = "IX_GuardianLinks_GuardianUserId_WardUserId";
}
