namespace Shared.Messaging.Contracts.Events.Family;

/// <summary>
/// An adult accepted a teen's nomination and is now their guardian. The teen is the one who has
/// been waiting, so unlike every other family acceptance this notification goes to the child.
/// <para>
/// <see cref="MinorActionTakenEvent"/> cannot carry it: that one is addressed to a guardian about
/// something their ward did, which is the opposite direction.
/// </para>
/// </summary>
public class GuardianNominationAcceptedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid NominationId { get; set; }

    /// <summary>The teen who sent the nomination, and who this notification is for.</summary>
    public Guid MinorUserId { get; set; }

    public Guid GuardianUserId { get; set; }
    public Guid HouseholdId { get; set; }

    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
