namespace Shared.Messaging.Contracts.Events.Events;

public record SeasonPlanPublishedEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid UserId { get; init; }  // recipient
    public required Guid PlanId { get; init; }
    public required Guid ClubId { get; init; }
    public required string PlanName { get; init; }
    public required string ClubName { get; init; }
    public required int EventCount { get; init; }
    public DateTime? FirstEventStartUtc { get; init; }
    public Guid? PublishedByUserId { get; init; }
    public string? PublishedByName { get; init; }
}
