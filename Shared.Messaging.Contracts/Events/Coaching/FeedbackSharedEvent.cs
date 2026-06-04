namespace Shared.Messaging.Contracts.Events.Coaching;

/// <summary>
/// Raised when a coach shares feedback with a player (on create-as-shared or
/// when toggling an existing private feedback to shared). Consumed by
/// notifications-service to notify the recipient.
/// </summary>
public record FeedbackSharedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; } // recipient (player)
    public required Guid FeedbackId { get; init; }
    public required Guid CoachUserId { get; init; }
    public required string CoachName { get; init; }
    public string? Preview { get; init; }
}
