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

    /// <summary>True when the invitation asks them to stand as a guardian rather than to be a
    /// child of the household. The two emails say different things.</summary>
    public bool IsGuardianInvitation { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
