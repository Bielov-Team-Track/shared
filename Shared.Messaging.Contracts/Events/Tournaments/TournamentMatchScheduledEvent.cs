namespace Shared.Messaging.Contracts.Events.Tournaments;

public record TournamentMatchScheduledEvent : IUserNotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required Guid MatchEventId { get; init; }
    public DateTime? StartTime { get; init; }
    public string? CourtName { get; init; }
    public int? CourtIndex { get; init; }
}
