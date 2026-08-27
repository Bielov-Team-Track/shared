namespace Shared.Messaging.Contracts.Events.Coaching;

/// <summary>
/// Raised when a coach edits feedback that is already shared with the player.
/// Consumed by notifications-service so the recipient learns it changed.
/// A private feedback raises nothing — the player has never seen it.
/// </summary>
public record FeedbackUpdatedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; } // recipient (player)
    public required Guid FeedbackId { get; init; }
    public required Guid CoachUserId { get; init; }
    public required string CoachName { get; init; }
    public string? Preview { get; init; }
}
