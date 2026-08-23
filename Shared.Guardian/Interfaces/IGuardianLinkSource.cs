using Shared.Enums;

namespace Shared.Guardian.Interfaces;

/// <summary>
/// The remote authority on guardianship — profiles-service, as every service adopting this
/// replica already reaches it. Each service adapts its own gRPC client to this; shared never
/// owns a gRPC channel.
/// </summary>
public interface IGuardianLinkSource
{
    /// <summary>Throws on transport failure — the caller decides what a failure means.</summary>
    Task<IReadOnlyList<Guid>> GetMinorsForGuardianAsync(Guid guardianId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetGuardiansForMinorAsync(Guid minorId, CancellationToken ct = default);

    /// <summary>Null means "could not ask" — never "no access". A null must leave an existing link alone.</summary>
    Task<GuardianLinkAccess?> CheckGuardianAccessAsync(Guid guardianId, Guid minorId, CancellationToken ct = default);
}

public sealed record GuardianLinkAccess(bool HasAccess, GuardianPermission Permissions,
                                        GuardianTier Tier = GuardianTier.Guardian);
