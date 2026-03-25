namespace Shared.Messaging.Contracts.Events.Auth;

public record UserDeletionScheduledEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required DateTime ScheduledDeletionAt { get; init; }
}
