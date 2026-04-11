namespace Shared.Messaging.Contracts.Events.Messages;

public record MessageCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid MessageId { get; init; }
    public Guid ChatId { get; init; }
    public Guid SenderId { get; init; }
}
