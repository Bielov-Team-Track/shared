using Shared.Messaging.Contracts.Events;

namespace Shared.Messaging.Contracts.Events.Auth;

public record StubUserCreatedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required Guid CreatedForClubId { get; init; }
}
