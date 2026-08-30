namespace Shared.Messaging.Contracts.Events.Tournaments;

public record TournamentMatchResultEvent : IUserNotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required Guid MatchEventId { get; init; }
    public Guid? WinnerEntrantId { get; init; }
    public string? WinnerName { get; init; }
    public int HomeScore { get; init; }
    public int AwayScore { get; init; }
}
