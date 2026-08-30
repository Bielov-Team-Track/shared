namespace Shared.Messaging.Contracts.Events.Tournaments;

public record TournamentEntrantInvitedEvent : IUserNotification
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
    public required Guid EntrantId { get; init; }
    public required string EntrantName { get; init; }
    public Guid? InvitedByUserId { get; init; }
}
