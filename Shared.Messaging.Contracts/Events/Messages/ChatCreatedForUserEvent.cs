namespace Shared.Messaging.Contracts.Events.Messages;

public record ChatCreatedForUserEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId { get; init; }
    public Guid ChatId { get; init; }
    public int ChatType { get; init; }
    public string? ChatTitle { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
}
