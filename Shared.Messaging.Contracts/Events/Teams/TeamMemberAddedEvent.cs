namespace Shared.Messaging.Contracts.Events.Teams;

public record TeamMemberAddedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }
    public required string AddedByUserName { get; init; }
}
