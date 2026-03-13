namespace Shared.Messaging.Contracts.Events.Family;

public class LinkRequestCreatedEvent : IEvent
{
    public Guid LinkRequestId { get; set; }
    public Guid GuardianUserId { get; set; }
    public Guid MinorUserId { get; set; }
    public DateTime ExpiresAt { get; set; }

    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
