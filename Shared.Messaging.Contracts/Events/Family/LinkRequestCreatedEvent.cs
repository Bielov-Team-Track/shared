using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Family;

/// <summary>
/// A family invitation reaching an address that already has an account, so it can be shown in the
/// app rather than emailed. Its counterpart for an address with no account behind it is
/// <see cref="FamilyInvitationEmailRequestedEvent"/>.
/// </summary>
public class LinkRequestCreatedEvent : IEvent
{
    public Guid LinkRequestId { get; set; }

    /// <summary>The sender. A guardian for the two invitation kinds; the child for a nomination.</summary>
    public Guid GuardianUserId { get; set; }

    /// <summary>Who the invitation is addressed to, and who the notification goes to. Not always a
    /// minor: a co-guardian invitation and a nomination are both addressed to an adult.</summary>
    public Guid MinorUserId { get; set; }

    /// <summary>What is being offered. Each kind says a different thing on a lock screen, and a
    /// nomination says the most different of the three — the sender is a child.</summary>
    public FamilyInvitationKind Kind { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
