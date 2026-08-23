using Shared.Contracts.Enums;

namespace Shared.Contracts.Mentions;

/// <summary>
/// What a group mention selects. Explicit because consumers used to infer it from
/// <see cref="GroupMentionTerm.Position"/> being null, which made @everyone the only
/// position-less term by accident — a second one would have silently behaved like it.
/// Switch on this, never on Position.HasValue.
/// </summary>
public enum GroupMentionKind
{
    Everyone,
    Position,
    Guardians
}

public record GroupMentionTerm(
    string Token,
    string Label,
    VolleyballPosition? Position,
    bool RequiresStaffRole,
    GroupMentionKind Kind = GroupMentionKind.Position)
{
    public IReadOnlyList<string> Aliases { get; init; } = [];
}

/// <summary>
/// The words a group mention can be written as, shared by every service that has to
/// agree on them. social-service parses them out of post HTML; messages-service
/// validates them against a chat message's offsets. A token that means one thing on
/// one side and another on the other is a message that displays "@oppos" and pings
/// nobody, so the table lives in one place.
///
/// Clients carry their own copy by necessity — this is the server's.
/// </summary>
public static class GroupMentionVocabulary
{
    public static IReadOnlyList<GroupMentionTerm> All { get; } =
    [
        new("everyone", "Everyone", null, RequiresStaffRole: true, GroupMentionKind.Everyone),
        new("guardians", "Guardians", null, RequiresStaffRole: true, GroupMentionKind.Guardians),
        new("setters", "Setters", VolleyballPosition.Setter, RequiresStaffRole: false, GroupMentionKind.Position),
        new("outsides", "Outsides", VolleyballPosition.OutsideHitter, RequiresStaffRole: false, GroupMentionKind.Position),
        new("opposites", "Opposites", VolleyballPosition.OppositeHitter, RequiresStaffRole: false, GroupMentionKind.Position)
        {
            Aliases = ["oppos"]
        },
        new("middles", "Middles", VolleyballPosition.MiddleBlocker, RequiresStaffRole: false, GroupMentionKind.Position),
        new("liberos", "Liberos", VolleyballPosition.Libero, RequiresStaffRole: false, GroupMentionKind.Position)
    ];

    private static readonly Dictionary<string, GroupMentionTerm> ByToken =
        All.SelectMany(term => term.Aliases.Append(term.Token).Select(token => (Token: token, Term: term)))
           .ToDictionary(entry => entry.Token, entry => entry.Term, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<VolleyballPosition, GroupMentionTerm> ByPosition =
        All.Where(term => term.Kind == GroupMentionKind.Position)
           .ToDictionary(term => term.Position!.Value);

    /// <summary>Case-insensitive, and accepts an alias as readily as the canonical token.</summary>
    public static bool TryResolveToken(string token, out GroupMentionTerm term)
        => ByToken.TryGetValue(token, out term!);

    public static GroupMentionTerm ForPosition(VolleyballPosition position) => ByPosition[position];
}
