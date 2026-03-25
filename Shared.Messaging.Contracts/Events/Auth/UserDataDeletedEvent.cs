namespace Shared.Messaging.Contracts.Events.Auth;

public record UserDataDeletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required string ServiceName { get; init; }
}
