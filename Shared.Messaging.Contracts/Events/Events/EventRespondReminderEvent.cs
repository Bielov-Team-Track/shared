namespace Shared.Messaging.Contracts.Events.Events;

public class EventRespondReminderEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId => RecipientUserId;

    public Guid RecipientUserId { get; set; }
    public Guid TargetEventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }

    /// <summary>
    /// What the organiser typed when they sent the nudge, if anything. Optional: the automatic
    /// reminder job has nobody to type it, and a manual send need not carry one.
    /// </summary>
    public string? ReminderMessage { get; set; }
}
