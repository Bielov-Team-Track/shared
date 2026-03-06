namespace Shared.Messaging.Contracts.Events.Auth;

public class UserActivatedEvent : IEvent
{
    public Guid UserId { get; set; }

    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
