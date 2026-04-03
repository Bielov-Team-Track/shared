namespace Shared.Messaging.Contracts.Events.Messages;

public record MessageSentEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId { get; init; }
    public Guid ChatId { get; init; }
    public Guid MessageId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string? ChatTitle { get; init; }
    public int ChatType { get; init; }
    public string? ContentPreview { get; init; }
    public bool HasAttachments { get; init; }
    public int AttachmentCount { get; init; }
    public DateTime SentAt { get; init; }
}
