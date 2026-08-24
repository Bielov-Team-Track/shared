using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Family;

/// <summary>
/// A family invitation addressed to an email that no account answers to yet. It carries no token
/// and no link into the flow on purpose: the recipient signs up like anyone else, and the
/// invitation finds them when they verify that address. The email exists only to tell them there
/// is something waiting.
/// <para>
/// Its counterpart for an address that already has an account is
/// <see cref="LinkRequestCreatedEvent"/>, which reaches them in the app.
/// </para>
/// </summary>
public class FamilyInvitationEmailRequestedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid InvitationId { get; set; }
    public Guid SenderUserId { get; set; }

    /// <summary>The address as typed by the sender, normalised. There is no user behind it.</summary>
    public string TargetEmail { get; set; } = string.Empty;

    /// <summary>What is being asked. Each kind's email says a different thing, and a nomination
    /// says the most different thing of the three — the sender is a child, not a parent.</summary>
    public FamilyInvitationKind Kind { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
