using Shared.Enums;

namespace Shared.Services;

/// <summary>
/// The remote authority on what a guardian may do — profiles-service, as each service already
/// reaches it. Shared never owns a gRPC channel: every service adapts its own client to this.
/// </summary>
public interface IGuardianAccessSource
{
    /// <summary>Null means "could not ask" — never "no access". The caller fails closed on null.</summary>
    Task<GuardianAccessSnapshot?> CheckAsync(
        Guid guardianUserId,
        Guid subjectUserId,
        IReadOnlyCollection<ConsentType>? requiredConsents,
        CancellationToken ct = default);
}

public sealed record GuardianAccessSnapshot(
    bool HasAccess,
    bool IsUnderRemovalNotice,
    GuardianPermission Permissions,
    IReadOnlySet<ConsentType> GrantedConsentTypes);
