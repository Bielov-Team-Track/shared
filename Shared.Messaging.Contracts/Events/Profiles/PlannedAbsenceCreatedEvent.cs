namespace Shared.Messaging.Contracts.Events.Profiles;

public record PlannedAbsenceCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid AbsenceId { get; init; }
    public required Guid UserId { get; init; }
    /// <summary>Midnight UTC, inclusive.</summary>
    public required DateTime FromDate { get; init; }
    /// <summary>Midnight UTC, INCLUSIVE — consumers must compare against ToDate.AddDays(1) exclusive.</summary>
    public required DateTime ToDate { get; init; }
    public required bool AutoDecline { get; init; }
}
