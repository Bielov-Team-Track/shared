namespace Shared.Messaging.Contracts.Events.Family;

public class GuardianConsentGrantedEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid MinorUserId { get; set; }
    public Guid GuardianUserId { get; set; }
    public Guid HouseholdId { get; set; }
    public List<string> ConsentTypes { get; set; } = new();
    public bool HasRequiredConsent { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
