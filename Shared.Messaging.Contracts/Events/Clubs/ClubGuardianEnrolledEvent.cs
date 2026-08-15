namespace Shared.Messaging.Contracts.Events.Clubs;

/// <summary>
/// A guardian was enrolled as a club member (Guardian role) as part of accepting
/// a child's targeted invitation — published to the enrolled guardian themselves
/// when someone else (the accepting co-guardian) enrolled them.
/// </summary>
public record ClubGuardianEnrolledEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required Guid ClubId { get; init; }
    public required string ClubName { get; init; }
    public required string ChildName { get; init; }
    public required string EnrolledByName { get; init; }
}
