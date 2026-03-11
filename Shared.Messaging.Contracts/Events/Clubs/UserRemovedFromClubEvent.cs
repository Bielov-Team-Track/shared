namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// Published by clubs-service when a user is removed from a club.
/// </summary>
public record UserRemovedFromClubEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
}
