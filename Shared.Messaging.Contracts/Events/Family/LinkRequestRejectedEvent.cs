namespace Shared.Messaging.Contracts.Events.Family;

public class LinkRequestRejectedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid GuardianUserId { get; set; }
    public Guid MinorUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
