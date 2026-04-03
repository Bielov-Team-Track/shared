namespace Shared.Messaging.Contracts.Events.Messages;

public record UserAddedToChatEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId { get; init; }
    public Guid ChatId { get; init; }
    public string? ChatTitle { get; init; }
    public int ChatType { get; init; }
    public Guid AddedByUserId { get; init; }
    public string AddedByName { get; init; } = string.Empty;
}
