namespace Shared.Messaging.Contracts.Events.Messages;

public record MessageRestoredEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid MessageId { get; init; }
    public Guid ChatId { get; init; }
}
