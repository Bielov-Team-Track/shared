namespace Shared.Enums;

/// <summary>
/// What a family invitation offers, as it travels between services. Profiles keeps its own
/// persisted enum — the column stores these names as strings and is the source of truth for the
/// flow — and maps onto this one at the boundary, so a rename inside profiles can never silently
/// change what an email says.
/// </summary>
public enum FamilyInvitationKind
{
    /// <summary>A guardian asking a teen to join their household as a child of it.</summary>
    Child = 0,

    /// <summary>A guardian asking an adult to stand alongside them for named children.</summary>
    CoGuardian = 1,

    /// <summary>A teen asking an adult to be their guardian. Sent by the child, not to them.</summary>
    GuardianNomination = 2
}
