namespace Shared.Messaging.Contracts.Events.Messages;

public record MessageReactionAddedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId { get; init; }
    public Guid ChatId { get; init; }
    public Guid MessageId { get; init; }
    public Guid ReactorUserId { get; init; }
    public string ReactorName { get; init; } = string.Empty;
    public string Emoji { get; init; } = string.Empty;
    public string? ContentPreview { get; init; }
}
