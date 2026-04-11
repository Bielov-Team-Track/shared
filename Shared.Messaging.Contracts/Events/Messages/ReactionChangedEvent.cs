namespace Shared.Messaging.Contracts.Events.Messages;

public record ReactionChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid MessageId { get; init; }
    public Guid ChatId { get; init; }
    public Guid UserId { get; init; }
    public string Emoji { get; init; } = string.Empty;
    public bool Added { get; init; }
}
