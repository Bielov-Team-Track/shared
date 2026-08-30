namespace Shared.Messaging.Contracts.Events.Tournaments;

public record TournamentEntrantInvitedEvent : IUserNotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }

    /// An organizer can invite a squad that has never heard of us, so for this notification an
    /// empty user list is the normal case rather than a missing one.
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];

    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string DeepLink { get; init; }
    public required string Category { get; init; }

    public required Guid TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required Guid EntrantId { get; init; }
    public required string EntrantName { get; init; }
    public Guid? InvitedByUserId { get; init; }

    /// The organizer's own words, shown above the accept button.
    public string? Message { get; init; }

    /// When the invitation stops being answerable.
    public DateTime? ExpiresAt { get; init; }
}
