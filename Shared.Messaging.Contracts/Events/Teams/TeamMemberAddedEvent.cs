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
    /// <summary>
    /// Who did the adding. Nullable because rows published before this field existed have no
    /// answer; <see cref="AddedByUserName"/> was a hardcoded "Admin" at every publish site, so the
    /// id is the only thing that can name a real person — or draw their avatar.
    /// </summary>
    public Guid? AddedByUserId { get; init; }

    public required string AddedByUserName { get; init; }
}
