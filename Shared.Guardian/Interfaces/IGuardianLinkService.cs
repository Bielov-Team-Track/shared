using Shared.Enums;

namespace Shared.Guardian.Interfaces;

public interface IGuardianLinkService
{
    Task UpsertAsync(Guid guardianId, Guid wardId, GuardianPermission permissions);
    Task RemoveAsync(Guid guardianId, Guid wardId);
    Task RemoveAllForWardAsync(Guid wardId);
    Task RemoveAllForUserAsync(Guid userId);

    /// <summary>
    /// Wards from the link rows alone. The link IS profiles-service's statement that this
    /// ward is a minor under guardianship, and profiles ends it at 18 (AdultTransitionJob) —
    /// re-deriving minority from a local DateOfBirth is what silently killed oversight for
    /// every null-DOB replica row in August. Access derivation uses THIS.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetWardIdsAsync(Guid guardianId, GuardianPermission required = GuardianPermission.None);

    /// <summary>
    /// GetWardIdsAsync additionally filtered to wards the LOCAL UserProfiles replica says are
    /// minors. Only messages-service's chat-oversight surface uses this — it preserves that
    /// service's existing semantics exactly. New callers should use GetWardIdsAsync.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetMinorWardIdsAsync(Guid guardianId, GuardianPermission required = GuardianPermission.None);

    /// <param name="force">
    /// Skip the one-hour success marker. The miss path must force: a warm marker otherwise
    /// hides a dropped grant for up to an hour. Forced calls are rate-limited to one per
    /// 60 s per user by a separate marker.
    /// </param>
    Task EnsureFreshAsync(Guid userId, bool force = false);
}
