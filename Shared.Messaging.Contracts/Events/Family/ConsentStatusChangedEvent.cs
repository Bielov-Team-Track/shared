namespace Shared.Messaging.Contracts.Events.Family;

public class ConsentStatusChangedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid MinorUserId { get; set; }
    public bool HasRequiredConsent { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
