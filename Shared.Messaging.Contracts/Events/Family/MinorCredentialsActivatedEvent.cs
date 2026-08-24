namespace Shared.Messaging.Contracts.Events.Family;

/// <summary>
/// A managed minor redeemed their guardian-issued invite and set a password. Their login is theirs
/// from this moment, which is what makes it no longer the guardian's to redirect: profiles refuses
/// to re-issue credentials for a minor whose login is active, so a guardian account in the wrong
/// hands cannot point a child's sign-in at an address the attacker controls.
/// <para>
/// Its counterpart <see cref="MinorCredentialsGrantedEvent"/> travels the other way, profiles to
/// auth, and asks for the invite to be sent.
/// </para>
/// </summary>
public class MinorCredentialsActivatedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid MinorUserId { get; set; }

    /// <summary>The address they signed in with — the one now fixed to the account.</summary>
    public string Email { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
