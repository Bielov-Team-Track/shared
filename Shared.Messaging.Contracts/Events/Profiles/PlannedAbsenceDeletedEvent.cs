namespace Shared.Messaging.Contracts.Events.Profiles;

public record PlannedAbsenceDeletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid AbsenceId { get; init; }
    public required Guid UserId { get; init; }
}
