namespace Shared.Messaging.Contracts.Events.Groups;

public record GroupMemberAddedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid GroupId { get; init; }
    public required string GroupName { get; init; }
    public required string AddedByUserName { get; init; }
}
