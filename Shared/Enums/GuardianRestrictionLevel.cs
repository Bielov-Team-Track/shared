namespace Shared.Enums;

/// <summary>
/// A club's decision about one derived guardian in one context. Ordered and cumulative: each
/// level is a strict superset of the one before, so every consumer compares with >= and no
/// combination of independent booleans can produce a state nobody designed.
///
/// This is NOT mute. A viewer muting a chat is a preference they set for themselves and it lives
/// where it always has (ChatParticipant.IsMuted, GuardianChatState.IsMuted). Silenced is a club
/// taking away the ability to speak, which is a different actor, a different lifecycle and a
/// different audit obligation.
/// </summary>
public enum GuardianRestrictionLevel
{
    /// <summary>No restriction. Never stored — the absence of an active row.</summary>
    None = 0,

    /// <summary>Reads everything the derivation grants; writes nothing — no message, no typing
    /// indicator, no post, no comment, no reaction, no poll vote.</summary>
    Silenced = 1,

    /// <summary>Silenced, and out of the context's chat entirely: not observed, not in fan-out,
    /// no pushes. The ward's participation is untouched. This is S25.</summary>
    ChatExcluded = 2,

    /// <summary>No derived standing in this context at all. The ward stays a member.</summary>
    Excluded = 3
}
